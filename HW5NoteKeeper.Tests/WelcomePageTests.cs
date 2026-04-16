using FluentAssertions;
using HW5NoteKeeper.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Integration tests for the welcome-screen requirement (requirement 4.1).
    /// Verifies that unauthenticated users see the NoteKeeper welcome text and that
    /// authenticated users are redirected to the Notes list.
    /// </summary>
    public class WelcomePageTests : IClassFixture<NoteKeeperWebApplicationFactory>
    {
        private readonly NoteKeeperWebApplicationFactory _factory;

        /// <summary>
        /// Initializes a new instance of <see cref="WelcomePageTests"/>.
        /// </summary>
        /// <param name="factory">The shared application factory with InMemory DB and TestAuth.</param>
        public WelcomePageTests(NoteKeeperWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ── Anonymous user tests ─────────────────────────────────────────────────

        /// <summary>
        /// An anonymous request to the root returns HTTP 200 — the page is publicly accessible.
        /// </summary>
        [Fact]
        public async Task AnonymousUser_GetHomePage_Returns200()
        {
            // No X-Test-User-Id header → TestAuthHandler returns NoResult → anonymous principal.
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "the home page is configured with AllowAnonymousToPage(\"/Index\") " +
                         "and must be accessible without authentication");
        }

        /// <summary>
        /// The welcome page body contains the exact text required by requirement 4.1.
        /// </summary>
        [Fact]
        public async Task AnonymousUser_GetHomePage_ShowsRequiredWelcomeText()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/");
            var html = await response.Content.ReadAsStringAsync();

            html.Should().Contain(
                "Welcome to the NoteKeeper application",
                because: "requirement 4.1 mandates this exact opening phrase");

            html.Should().Contain(
                "This application allows users to manage their notes",
                because: "requirement 4.1 mandates this description sentence");

            html.Should().Contain(
                "Please login to manage your notes",
                because: "requirement 4.1 mandates this call-to-action phrase");
        }

        /// <summary>
        /// The welcome page contains a sign-in link so the user knows how to log in.
        /// </summary>
        [Fact]
        public async Task AnonymousUser_GetHomePage_ContainsSignInLink()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/");
            var html = await response.Content.ReadAsStringAsync();

            html.Should().ContainAny(new[] { "Sign in", "SignIn" },
                because: "the welcome page should offer a clear path for the user to log in");
        }

        // ── Authenticated user tests ─────────────────────────────────────────────

        /// <summary>
        /// An authenticated user who navigates to the home page is immediately redirected
        /// to <c>/Notes/Index</c> so they land directly on their notes list.
        /// </summary>
        [Fact]
        public async Task AuthenticatedUser_GetHomePage_RedirectsToNotesIndex()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "test-user-oid");

            var response = await client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                because: "authenticated users should be sent directly to their notes");

            response.Headers.Location!.ToString().Should().Contain("/Notes",
                because: "the redirect destination must be the Notes list page");
        }

        /// <summary>
        /// After following the home-page redirect, an authenticated user lands on the Notes page
        /// with HTTP 200 — confirming the full journey works end-to-end.
        /// </summary>
        [Fact]
        public async Task AuthenticatedUser_GetHomePage_FollowsRedirectToNotesWithSuccess()
        {
            // AllowAutoRedirect = true (default) so the client follows the 302.
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "test-user-oid");

            var response = await client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "after following the redirect the Notes list page should load successfully");

            var html = await response.Content.ReadAsStringAsync();
            // The Notes/Index page renders a table or a 'no notes' message — either way,
            // it should NOT contain the unauthenticated welcome phrase.
            html.Should().NotContain(
                "Please login to manage your notes",
                because: "authenticated users see the Notes list, not the anonymous welcome screen");
        }
        // ── Post-logout state tests (Requirement 5.2) ───────────────────────────
        // After a user logs out, Entra redirects back to the application root ("/")
        // via the configured SignedOutRedirectUri. The user is then unauthenticated,
        // so the tests below mirror exactly what that user sees: requirement 5.2.1-5.2.3.

        /// <summary>
        /// After logout the user arrives at the home page as an anonymous visitor
        /// and sees the required welcome text (requirement 5.2.1 / 4.1).
        /// </summary>
        [Fact]
        public async Task AfterLogout_WelcomePage_ShowsRequiredWelcomeText()
        {
            // Simulate post-logout: no auth header = anonymous, just like the OIDC
            // redirect landing will be after SignedOutRedirectUri sends the user to "/".
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/");
            var html = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "post-logout the root path must be reachable without authentication");

            html.Should().Contain("Welcome to the NoteKeeper application",
                because: "requirement 5.2.1 states the welcome screen must be shown after logout");
            html.Should().Contain("Please login to manage your notes",
                because: "requirement 5.2.1 states the welcome screen must be shown after logout");
        }

        /// <summary>
        /// After logout the sign-in link must appear in the top navigation bar
        /// (requirement 5.2.2: "a link in the top navigation area").
        /// </summary>
        [Fact]
        public async Task AfterLogout_TopNavigation_ContainsSignInLink()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/");
            var html = await response.Content.ReadAsStringAsync();

            // Extract the <nav>…</nav> block from the rendered layout.
            var navMatch = Regex.Match(html, @"<nav\b[^>]*>(.*?)</nav>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            navMatch.Success.Should().BeTrue(
                because: "the layout must render a <nav> element containing the navigation bar");

            var navHtml = navMatch.Groups[1].Value;

            navHtml.Should().Contain("Sign in",
                because: "requirement 5.2.2 mandates a login link in the top navigation area " +
                         "so the user can sign back in after being redirected to the welcome screen");
        }

        /// <summary>
        /// After logout the top navigation bar does NOT show a sign-out link or the user's
        /// name — confirming the session was fully cleared.
        /// </summary>
        [Fact]
        public async Task AfterLogout_TopNavigation_DoesNotShowSignOutOrUsername()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/");
            var html = await response.Content.ReadAsStringAsync();

            var navMatch = Regex.Match(html, @"<nav\b[^>]*>(.*?)</nav>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var navHtml = navMatch.Success ? navMatch.Groups[1].Value : html;

            navHtml.Should().NotContain("Sign out",
                because: "an unauthenticated (post-logout) user must not see a sign-out link");
        }

        /// <summary>
        /// With auto-redirect enabled, an anonymous request to the root still returns the
        /// welcome page content — proving no redirect loop or OIDC challenge occurs.
        /// This mirrors the real browser experience on first app startup.
        /// </summary>
        [Fact]
        public async Task AnonymousUser_GetHomePage_WithAutoRedirect_StillShowsWelcomePage()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
            });

            var response = await client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "even with redirects enabled, an anonymous visit to '/' must land " +
                         "on the welcome page, not trigger an auth challenge");

            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Welcome to the NoteKeeper application",
                because: "the final rendered page must be the welcome screen, not a login page");
        }

        /// <summary>
        /// Verifies requirement 5.2.3: the OIDC post-logout redirect URI points to the
        /// application root ("/") so the user lands back on the welcome page, not on Entra's
        /// sign-out confirmation page.
        /// This test is a companion to <see cref="OpenIdConnectConfigurationTests.OpenIdConnectOptions_SignedOutRedirectUri_IsHomePage"/>.
        /// </summary>
        [Fact]
        public async Task AfterLogout_HomePageIsAccessibleAsLandingPage()
        {
            // Confirms that "/" responds with 200 for an unauthenticated visitor — i.e., the
            // destination that SignedOutRedirectUri points to is actually reachable.
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "requirement 5.2.3 requires redirection back to the application; " +
                         "the destination page must return 200, not a redirect or error");
        }
    }
}
