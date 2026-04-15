using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Middleware;
using HW5NoteKeeperSolution.Services;
using HW5NoteKeeperSolution.Settings;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Security.Claims;
using MSFTBuilder = Microsoft.AspNetCore.Builder;

namespace HW5NoteKeeperSolution
{
    /// <summary>
    /// Application entry point. Configures the ASP.NET Core host, registers all services,
    /// applies EF Core migrations, and starts the web application.
    /// </summary>
    public partial class Program
    {
        /// <summary>
        /// The application entry point. Builds and runs the web host asynchronously.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the host builder.</param>
        public static async Task Main(string[] args)
        {
            var builder = MSFTBuilder.WebApplication.CreateBuilder(args);

            using var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddConsole();
                logging.AddAzureWebAppDiagnostics();
            });

            var startupLogger = loggerFactory.CreateLogger("Program");
            startupLogger.LogInformationWithCallerInfo("Starting application...");

            var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>();
 
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not found and is required.");

            builder.Services.AddDbContext<NoteKeeperContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";

                // After a successful sign-out, redirect the user to the welcome page.
                // The OIDC middleware calls this URI once the signout callback from Entra completes.
                options.SignedOutRedirectUri = "/";

                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var identity = context.Principal?.Identity as ClaimsIdentity;

                    if (identity == null)
                    {
                        logger.LogWarning("User login completed, but no claims identity was available to log.");
                        return Task.CompletedTask;
                    }

                    logger.LogInformationWithCallerInfo($"User login completed. Logging {identity.Claims.Count()} claims.");

                    foreach (var claim in identity.Claims)
                    {
                        logger.LogInformationWithCallerInfo($"Claim type: {claim.Type}; value: {claim.Value}");
                    }

                    return Task.CompletedTask;
                };
            });

            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
                .AddInMemoryTokenCaches();

            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
            });

            builder.Services.AddMemoryCache();
              
            AISettings aiSettings = builder.Configuration.GetSection("AzureOpenAI").Get<AISettings>()
                ?? throw new InvalidOperationException("AzureOpenAI configuration is required.");
            ValidateAISettings(aiSettings);
            builder.Services.AddSingleton(aiSettings);

            NoteLimits noteLimits = builder.Configuration.GetSection("NoteLimits").Get<NoteLimits>() ?? new NoteLimits();
            builder.Services.AddSingleton(noteLimits);

            StorageOperationalSettings storageOperationalSettings = builder.Configuration
                .GetSection("StorageOperationalSettings")
                .Get<StorageOperationalSettings>() ?? new StorageOperationalSettings();
            builder.Services.AddSingleton(storageOperationalSettings);

            StorageAccountSettings storageAccountSettings = builder.Configuration
                .GetSection("StorageAccountSettings")
                .Get<StorageAccountSettings>()
                ?? throw new InvalidOperationException("StorageAccountSettings configuration is required.");
            ValidateStorageSettings(storageAccountSettings);
            builder.Services.AddSingleton(storageAccountSettings);

            builder.Services.AddSingleton(_ => CreateAzureCredential(builder.Environment, storageAccountSettings.TenantId));
            builder.Services.AddSingleton(sp =>
                new BlobServiceClient(
                    new Uri(storageAccountSettings.Url),
                    sp.GetRequiredService<DefaultAzureCredential>()));
            builder.Services.AddSingleton(sp =>
                new QueueServiceClient(
                    BuildQueueEndpoint(storageAccountSettings),
                    sp.GetRequiredService<DefaultAzureCredential>()));

            builder.Services.AddSingleton<IChatClient>(sp =>
            {
                Uri deploymentUri = new Uri(aiSettings.DeploymentUri);
                AzureOpenAIClient client = string.IsNullOrWhiteSpace(aiSettings.ApiKey)
                    ? new AzureOpenAIClient(deploymentUri, sp.GetRequiredService<DefaultAzureCredential>())
                    : new AzureOpenAIClient(deploymentUri, new AzureKeyCredential(aiSettings.ApiKey));

                return client.GetChatClient(aiSettings.DeploymentModelName).AsIChatClient();
            });

            builder.Services.AddSingleton<IAzureStorageInitializer, AzureStorageInitializer>();
            builder.Services.AddScoped<IDatabaseSchemaInitializer, DatabaseSchemaInitializer>();
            builder.Services.AddScoped<ITagGeneratorService, TagGeneratorService>();
            builder.Services.AddScoped<INoteTagService, NoteTagService>();
            builder.Services.AddScoped<IUserNoteSeedService, UserNoteSeedService>();

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AllowAnonymousToPage("/Index");
            })
            .AddMicrosoftIdentityUI();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var schemaInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseSchemaInitializer>();
                await schemaInitializer.InitializeAsync();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<FirstLoginSeedingMiddleware>();
            app.UseAuthorization();

            app.MapStaticAssets().AllowAnonymous();
            app.MapRazorPages().WithStaticAssets();
            app.MapControllers();

            app.Run();
        }

        private static DefaultAzureCredential CreateAzureCredential(IHostEnvironment environment, string tenantId)
        {
            var options = new DefaultAzureCredentialOptions
            {
                SharedTokenCacheTenantId = tenantId,
                VisualStudioCodeTenantId = tenantId,
                VisualStudioTenantId = tenantId
            };

            if (environment.IsDevelopment())
            {
                options.ExcludeManagedIdentityCredential = true;
                options.ExcludeWorkloadIdentityCredential = true;
            }

            return new DefaultAzureCredential(options);
        }

        private static Uri BuildQueueEndpoint(StorageAccountSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.QueueEndpoint))
            {
                return new Uri(settings.QueueEndpoint);
            }

            return new Uri(settings.Url.Replace(".blob.", ".queue.", StringComparison.OrdinalIgnoreCase));
        }

        private static void ValidateAISettings(AISettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.DeploymentUri))
            {
                throw new InvalidOperationException("AzureOpenAI:DeploymentUri is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.DeploymentModelName))
            {
                throw new InvalidOperationException("AzureOpenAI:DeploymentModelName is required.");
            }
        }

        private static void ValidateStorageSettings(StorageAccountSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Url))
            {
                throw new InvalidOperationException("StorageAccountSettings:Url is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.TenantId))
            {
                throw new InvalidOperationException("StorageAccountSettings:TenantId is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.AccountName))
            {
                throw new InvalidOperationException("StorageAccountSettings:AccountName is required.");
            }
        }
    }
}
