using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Win32;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Principal;
using MSFTBuilder = Microsoft.AspNetCore.Builder;

namespace HW5NoteKeeperSolution
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = MSFTBuilder.WebApplication.CreateBuilder(args);

            // Build a startup logger early so the application can report progress before the final
            // host is created. This is helpful when diagnosing configuration or authentication issues
            // that happen during service registration.
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddAzureWebAppDiagnostics();
            });
            var startupLogger = loggerFactory.CreateLogger("Program");
            startupLogger.LogInformationWithCallerInfo("Starting application..."); 
            
            //var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ') ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ');
            // The home page calls Microsoft Graph for the signed-in user's profile.
            // We derive the downstream scopes from configuration here so the auth pipeline can
            // request consent and create a GraphServiceClient with the correct delegated scopes.
            var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>();

            builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, (OpenIdConnectOptions options) =>
            {
                // Keep the OpenID Connect claim names exactly as the identity provider issues them.
                // Without this setting, ASP.NET Core remaps many standard OIDC claim names to older
                // WS-Federation style URIs such as ClaimTypes.Name or ClaimTypes.Surname. For this
                // demo we want students to see the raw claims from Entra in the UI, including
                // "name", "given_name", and "family_name".
                options.MapInboundClaims = false;

                // Once inbound mapping is disabled, ClaimsIdentity.Name is no longer inferred from a
                // remapped URI claim. Setting NameClaimType tells token validation to treat the raw
                // OIDC "name" claim as the display name for the signed-in user, which keeps
                // User.Identity.Name aligned with what students see in the claims table.
                options.TokenValidationParameters.NameClaimType = "name";

                // Hook the token-validated event so we can inspect the final authenticated principal.
                // Logging claims here gives one complete dump per successful login, which is much more
                // useful than logging claims on every request.
                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var identity = context.Principal?.Identity as ClaimsIdentity;

                    // If the principal is unexpectedly missing, log the problem and continue rather
                    // than throwing. Authentication already succeeded, so this log is intended to
                    // surface an unexpected state for investigation.
                    if (identity == null)
                    {
                        logger.LogWarning("User login completed, but no claims identity was available to log.");
                        return Task.CompletedTask;
                    }

                    // Emit every claim key/value pair so the demo can confirm exactly which claim
                    // types Entra External ID and the ASP.NET Core handler expose at runtime.
                    logger.LogInformationWithCallerInfo($"User login completed. Logging {identity.Claims.Count()} claims.");

                    foreach (var claim in identity.Claims)
                    {
                        logger.LogInformationWithCallerInfo($"Claim type: {claim.Type}; value: {claim.Value}");
                    }

                    return Task.CompletedTask;
                }; 
            });

            // Add services to the container.
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
                // The original failure happened because authentication was configured, but the
                // downstream token acquisition/Graph services were not. That left the page model
                // without a registered GraphServiceClient to inject.
                .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
                .AddInMemoryTokenCaches();

            builder.Services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy.
                options.FallbackPolicy = options.DefaultPolicy;
            });

            builder.Services.AddRazorPages(options =>
            {
                // The landing page is intentionally public for the demo.
                // With a fallback policy in place, any page that should remain public must be
                // explicitly marked anonymous here or with an [AllowAnonymous] attribute.
                //options.Conventions.AllowAnonymousToPage("/Index");
            })
            .AddMicrosoftIdentityUI();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            //app.MapStaticAssets();
            // MapStaticAssets uses endpoint routing, build-time compression, fingerprinting, and
            // optimized cache headers, so it is the higher-performance static asset option for
            // modern ASP.NET Core apps.
            //
            // Because static assets are endpoints, the fallback authorization policy would also
            // protect CSS, JavaScript, and image requests unless we override it. AllowAnonymous()
            // keeps the landing page and Microsoft Identity UI pages able to load their assets.
            //
            // If a future app needs protected files, keep those in a separate location and secure
            // that location explicitly instead of making the shared wwwroot assets authenticated.
            app.MapStaticAssets().AllowAnonymous();

            //app.MapRazorPages()
            //   .WithStaticAssets();
            // Map the Razor Pages endpoints for the application. The fallback policy secures them
            // by default, except for pages explicitly marked anonymous.
            app.MapRazorPages()
             .WithStaticAssets();

            app.MapControllers();

            app.Run();
        }
    } 
}
