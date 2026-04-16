using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Services;
using HW5NoteKeeperSolution.Settings;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
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

            // Strip authentication-related properties from the connection string.
            // We handle Azure AD token acquisition via our own AadAuthInterceptor using a
            // DefaultAzureCredential that excludes InteractiveBrowserCredential, preventing
            // the "Pick an account" popup from ever appearing.
            var csBuilder = new SqlConnectionStringBuilder(connectionString);
            csBuilder.Remove("Authentication");
            csBuilder.Remove("User ID");
            csBuilder.Remove("UID");
            csBuilder.Remove("Password");
            csBuilder.Remove("PWD");
            connectionString = csBuilder.ToString();

            builder.Services.AddDbContext<NoteKeeperContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(new AadAuthInterceptor(sp.GetRequiredService<DefaultAzureCredential>()));
            });

            builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";

                // After a successful sign-out, redirect the user to the welcome page.
                // The OIDC middleware calls this URI once the signout callback from Entra completes.
                options.SignedOutRedirectUri = "/";

                // Skip "Pick an account" — always go straight to the login/signup form.
                options.Prompt = "login";

                options.Events ??= new OpenIdConnectEvents();

                // Capture any handler already registered by Microsoft.Identity.Web
                // (e.g., for the Forgot-Password B2C policy redirect) so we can chain it.
                var existingOnRemoteFailure = options.Events.OnRemoteFailure;
                options.Events.OnRemoteFailure = async context =>
                {
                    // Let Microsoft.Identity.Web's own handler run first (if any).
                    if (existingOnRemoteFailure != null)
                        await existingOnRemoteFailure(context);

                    // If the failure has not already been handled (e.g., by a B2C policy
                    // redirect), redirect the user gracefully back to the welcome page.
                    // This covers cases such as a non-tenant account attempting to sign in.
                    if (context.Result?.Handled != true)
                    {
                        var failureLogger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        failureLogger.LogWarningWithCallerInfo(
                            $"OIDC remote failure: {context.Failure?.Message}. " +
                            "Redirecting to welcome page.");
                        context.HandleResponse();
                        context.Response.Redirect("/");
                    }
                };

                // Pass logout_hint so Entra signs out directly without "Pick an account"
                var existingOnSignOut = options.Events.OnRedirectToIdentityProviderForSignOut;
                options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
                {
                    if (existingOnSignOut != null)
                        await existingOnSignOut(context);

                    var hint = context.HttpContext.User.FindFirst("login_hint")?.Value
                        ?? context.HttpContext.User.FindFirst("preferred_username")?.Value
                        ?? context.HttpContext.User.FindFirst("email")?.Value;

                    if (!string.IsNullOrEmpty(hint))
                        context.ProtocolMessage.SetParameter("logout_hint", hint);
                };

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
            builder.Services.AddScoped<INoteTagService, NoteTagService>();
            builder.Services.AddScoped<IUserNoteSeedService, UserNoteSeedService>();

            builder.Services.AddRazorPages(options =>
            {
                // Require authentication for the app by default. Individual pages that should stay
                // public must be opted out explicitly below.
                options.Conventions.AuthorizeFolder("/");

                // Keep the landing page public so anonymous users can reach the app before choosing
                // to sign in. The error page is also public so failures can be rendered cleanly
                // without triggering an auth challenge redirect loop.
                options.Conventions.AllowAnonymousToPage("/Index");
                options.Conventions.AllowAnonymousToPage("/Error");
            })
            .AddMicrosoftIdentityUI();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
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
                VisualStudioTenantId = tenantId,
                ExcludeInteractiveBrowserCredential = true
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
