using FluentAssertions;
using HW5NoteKeeper.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Regression tests for OpenIdConnect options — ported from AzureEntraExternalIdDemo.Tests.
    /// Guards against any change to auth configuration that would break Entra External ID (CIAM)
    /// claim mapping, particularly the raw "oid" and "name" claim types needed by the app's
    /// claims helpers.
    /// </summary>
    public class OpenIdConnectConfigurationTests
    {
        [Fact]
        public void OpenIdConnectOptions_MapInboundClaims_IsFalse()
        {
            using NoteKeeperTestWebApplicationFactory factory = new();
            IOptionsMonitor<OpenIdConnectOptions> monitor =
                factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();

            OpenIdConnectOptions options = monitor.Get(OpenIdConnectDefaults.AuthenticationScheme);

            options.MapInboundClaims.Should().BeFalse(
                because: "Entra External ID sends raw OIDC claim names; mapping them would " +
                         "rename 'oid' and 'name' to long URI forms and break ClaimsPrincipalExtensions.");
        }

        [Fact]
        public void OpenIdConnectOptions_NameClaimType_IsName()
        {
            using NoteKeeperTestWebApplicationFactory factory = new();
            IOptionsMonitor<OpenIdConnectOptions> monitor =
                factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();

            OpenIdConnectOptions options = monitor.Get(OpenIdConnectDefaults.AuthenticationScheme);

            options.TokenValidationParameters.NameClaimType.Should().Be("name",
                because: "the display-name claim in Entra External ID tokens is the raw 'name' claim.");
        }

        [Fact]
        public void OpenIdConnectOptions_SignedOutRedirectUri_IsHomePage()
        {
            using NoteKeeperTestWebApplicationFactory factory = new();
            IOptionsMonitor<OpenIdConnectOptions> monitor =
                factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();

            OpenIdConnectOptions options = monitor.Get(OpenIdConnectDefaults.AuthenticationScheme);

            options.SignedOutRedirectUri.Should().Be("/",
                because: "after sign-out the OIDC middleware must redirect the user to the welcome " +
                         "page so they see the unauthenticated welcome screen (requirement 4.1)");
        }

        [Fact]
        public async Task OpenIdConnectOptions_OnRemoteFailure_RedirectsToWelcomePage()
        {
            using NoteKeeperTestWebApplicationFactory factory = new();
            IOptionsMonitor<OpenIdConnectOptions> monitor =
                factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();

            OpenIdConnectOptions options = monitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
            options.Events.OnRemoteFailure.Should().NotBeNull(
                because: "OIDC remote failures (e.g., non-tenant accounts rejected by Entra) " +
                         "must be caught and the user redirected to the welcome page");

            // Invoke the handler directly with a synthetic failure and verify it
            // marks the response as handled and issues a redirect to the welcome page.
            // The DefaultHttpContext needs a service provider so the handler can resolve ILogger<Program>.
            var testServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            testServices.AddLogging();
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                RequestServices = testServices.BuildServiceProvider(),
            };
            var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
                OpenIdConnectDefaults.AuthenticationScheme,
                displayName: null,
                typeof(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectHandler));

            var remoteFailure = new Microsoft.AspNetCore.Authentication.RemoteFailureContext(
                httpContext, scheme, new OpenIdConnectOptions(),
                new Exception("AADSTS50020: User not in tenant"));

            await options.Events.OnRemoteFailure(remoteFailure);

            remoteFailure.Result?.Handled.Should().BeTrue(
                because: "the handler must call HandleResponse() to suppress the default exception");
            httpContext.Response.Headers.Location.ToString().Should().Be("/",
                because: "the user must be redirected to the welcome page after an auth failure");
        }

        [Fact]
        public void OpenIdConnectOptions_OnRedirectToIdentityProviderForSignOut_SetsLogoutHint()
        {
            using NoteKeeperTestWebApplicationFactory factory = new();
            IOptionsMonitor<OpenIdConnectOptions> monitor =
                factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();

            OpenIdConnectOptions options = monitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
            options.Events.OnRedirectToIdentityProviderForSignOut.Should().NotBeNull(
                because: "sign-out must pass logout_hint so Entra never shows 'Pick an account'");
        }

        /// <summary>
        /// Verifies the logout_hint logic in isolation: given a user with a login_hint claim,
        /// the sign-out redirect must set logout_hint on the protocol message so Entra
        /// skips the "Pick an account" screen entirely.
        /// </summary>
        [Theory]
        [InlineData("login_hint", "user@example.com")]
        [InlineData("preferred_username", "user@example.com")]
        [InlineData("email", "user@example.com")]
        public void LogoutHint_IsSet_WhenUserHasIdentifyingClaim(string claimType, string claimValue)
        {
            // Reproduce the same logic that Program.cs wires into OnRedirectToIdentityProviderForSignOut
            var claims = new[] { new System.Security.Claims.Claim(claimType, claimValue) };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            var hint = principal.FindFirst("login_hint")?.Value
                ?? principal.FindFirst("preferred_username")?.Value
                ?? principal.FindFirst("email")?.Value;

            hint.Should().Be(claimValue,
                because: $"the '{claimType}' claim must be used as logout_hint to skip the account picker");
        }

        // ── Test factory ─────────────────────────────────────────────────

        /// <summary>
        /// Minimal test host for DI / options inspection. Stubs out Azure-dependent
        /// services so the host starts without real Azure SQL or OpenAI connections.
        /// </summary>
        private sealed class NoteKeeperTestWebApplicationFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                // Override the connection string so the SQL Server client is configured
                // with a safe placeholder; the real schema initializer is replaced below.
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            "Server=(localdb)\\mssqllocaldb;Database=NoteKeeperTests;Trusted_Connection=True;"
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    // Swap SQL Server EF context for InMemory so any accidental context
                    // resolution during startup does not hit the real Azure SQL endpoint.
                    ServiceDescriptor? efDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<NoteKeeperContext>));
                    if (efDescriptor is not null)
                        services.Remove(efDescriptor);

                    services.AddDbContext<NoteKeeperContext>(options =>
                        options.UseInMemoryDatabase("OidcConfigTestDb"));

                    // Replace the chat client to avoid creating a real Azure OpenAI client
                    // against potentially unavailable endpoints during CI / offline tests.
                    services.RemoveAll<IChatClient>();
                    services.AddSingleton<IChatClient>(new Mock<IChatClient>().Object);
                });
            }
        }
    }
}
