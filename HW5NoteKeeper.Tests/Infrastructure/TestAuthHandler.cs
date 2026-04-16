using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace HW5NoteKeeper.Tests.Infrastructure
{
    /// <summary>
    /// Replaces the real OIDC/cookie authentication in integration tests.
    /// Reads <see cref="UserIdHeader"/> from the request; if present, authenticates
    /// the request as that user with the ObjectId claim used by <c>User.GetObjectIdentifier()</c>.
    /// </summary>
    public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestAuth";
        public const string UserIdHeader = "X-Test-User-Id";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <summary>Prefix prepended to the user-ID to form the test display name ("name" claim).</summary>
        public const string DisplayNamePrefix = "Test User:";

        /// <summary>Domain appended to the user-ID to form the test email address.</summary>
        public const string EmailDomain = "@notekeeper.test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserIdHeader, out var values)
                || string.IsNullOrEmpty(values.FirstOrDefault()))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var userId = values.First()!;
            var email = userId + EmailDomain;

            var claims = new[]
            {
                new Claim(AzureADClaimTypes.ObjectId, userId),
                // "name" claim: the display-name shown in the top nav (ClaimName() fallback + NameClaimType).
                new Claim("name", DisplayNamePrefix + " " + userId),
                // preferred_username / ClaimTypes.Name — email-like, mirrors Entra External ID token.
                new Claim(ClaimTypes.Name, email),
                // Explicit email claim read by ClaimsPrincipalExtensions.GetEmailAddress().
                new Claim(ClaimTypes.Email, email),
            };

            // nameType = "name" mirrors TokenValidationParameters.NameClaimType = "name"
            // configured in Program.cs, making User.Identity.Name == the display-name claim.
            var identity = new ClaimsIdentity(claims, SchemeName, nameType: "name", roleType: null);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
