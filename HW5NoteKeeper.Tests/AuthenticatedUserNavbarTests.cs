using FluentAssertions;
using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Integration tests for the authenticated-user requirements (5.1.x).
    /// Verifies that when a user is logged in the top navigation bar shows their
    /// display name and email address, a sign-out link, and that the Notes list
    /// does not expose any internal multi-tenant data.
    /// </summary>
    /// <remarks>
    /// All tests use <see cref="NoteKeeperWebApplicationFactory"/> (InMemory DB + TestAuth)
    /// to drive the full HTTP pipeline without requiring a live Entra tenant.
    /// </remarks>
    public class AuthenticatedUserNavbarTests
        : IClassFixture<NoteKeeperWebApplicationFactory>, IAsyncLifetime
    {
        private readonly NoteKeeperWebApplicationFactory _factory;

        private const string UserId = "integration-user-oid";

        /// <summary>The display name rendered by <c>TestAuthHandler</c> for the test user.</summary>
        private static readonly string ExpectedDisplayName =
            TestAuthHandler.DisplayNamePrefix + " " + UserId;

        /// <summary>The email address rendered by <c>TestAuthHandler</c> for the test user.</summary>
        private static readonly string ExpectedEmail =
            UserId + TestAuthHandler.EmailDomain;

        /// <summary>Initializes a new instance of <see cref="AuthenticatedUserNavbarTests"/>.</summary>
        /// <param name="factory">The shared application factory with InMemory DB and TestAuth.</param>
        public AuthenticatedUserNavbarTests(NoteKeeperWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>Clears the database before each test for full isolation.</summary>
        public async Task InitializeAsync() => await _factory.ClearDatabaseAsync();

        /// <summary>No per-test teardown is required.</summary>
        public Task DisposeAsync() => Task.CompletedTask;

        // ── Helpers ──────────────────────────────────────────────────────────────

        private HttpClient CreateAuthenticatedClient()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, UserId);
            return client;
        }

        /// <summary>
        /// Extracts the first <c>&lt;nav&gt;…&lt;/nav&gt;</c> block from a full HTML page.
        /// </summary>
        private static string ExtractNavHtml(string html)
        {
            var match = Regex.Match(html, @"<nav\b[^>]*>(.*?)</nav>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>
        /// Extracts the <c>&lt;tbody&gt;…&lt;/tbody&gt;</c> block from the notes-list table.
        /// </summary>
        private static string ExtractTbodyHtml(string html)
        {
            var match = Regex.Match(html, @"<tbody\b[^>]*>(.*?)</tbody>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        // ── 5.1.1 — Display name ─────────────────────────────────────────────────

        /// <summary>
        /// Requirement 5.1.1: the authenticated user's display name appears in the top navigation bar.
        /// </summary>
        [Fact]
        public async Task WhenLoggedIn_TopNavigation_ShowsUserDisplayName()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("/Notes/Index");
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "an authenticated user must be able to access the Notes list");

            var html = await response.Content.ReadAsStringAsync();
            var navHtml = ExtractNavHtml(html);

            navHtml.Should().NotBeEmpty(
                because: "the layout must render a <nav> element");

            navHtml.Should().Contain(ExpectedDisplayName,
                because: "requirement 5.1.1 states the user's name must be displayed in the top nav");
        }

        // ── 5.1.1 — Email address ────────────────────────────────────────────────

        /// <summary>
        /// Requirement 5.1.1: the authenticated user's email address appears in the top navigation bar.
        /// </summary>
        [Fact]
        public async Task WhenLoggedIn_TopNavigation_ShowsUserEmailAddress()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("/Notes/Index");
            var html = await response.Content.ReadAsStringAsync();
            var navHtml = ExtractNavHtml(html);

            navHtml.Should().Contain(ExpectedEmail,
                because: "requirement 5.1.1 states the user's email address must be displayed in the top nav");
        }

        // ── 5.1.2 — Sign-out link ────────────────────────────────────────────────

        /// <summary>
        /// Requirement 5.1.2: a sign-out link is present in the top navigation bar for an authenticated user.
        /// </summary>
        [Fact]
        public async Task WhenLoggedIn_TopNavigation_ShowsSignOutLink()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("/Notes/Index");
            var html = await response.Content.ReadAsStringAsync();
            var navHtml = ExtractNavHtml(html);

            navHtml.Should().Contain("Sign out",
                because: "requirement 5.1.2 requires a sign-out link in the top navigation area");
        }

        /// <summary>
        /// Requirement 5.1.2 (inverse): the sign-in link must NOT appear in the nav
        /// once the user is already authenticated.
        /// </summary>
        [Fact]
        public async Task WhenLoggedIn_TopNavigation_DoesNotShowSignInLink()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("/Notes/Index");
            var html = await response.Content.ReadAsStringAsync();
            var navHtml = ExtractNavHtml(html);

            navHtml.Should().NotContain("Sign in",
                because: "an authenticated user's nav must not show a redundant sign-in link");
        }

        // ── 5.1.3 — Notes list visible ───────────────────────────────────────────

        /// <summary>
        /// Requirement 5.1.3: after login the user sees their list of notes.
        /// </summary>
        [Fact]
        public async Task WhenLoggedIn_NotesIndex_ShowsNotesListPage()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("/Notes/Index");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "requirement 5.1.3 states the user must see their notes list after login");

            var html = await response.Content.ReadAsStringAsync();
            html.Should().NotContain("Please login to manage your notes",
                because: "authenticated users see the notes list, not the anonymous welcome screen");
        }

        // ── 5.1.4 — No internal multi-tenant data in the list ───────────────────

        /// <summary>
        /// Requirement 5.1.4: the notes list must NOT display the internal <c>UserRealmId</c>
        /// (the user's Entra object identifier used for multi-tenant filtering).
        /// The table should show only user-facing fields: Summary, Details, dates, Tags, and CRUD links.
        /// </summary>
        [Fact]
        public async Task WhenLoggedIn_NotesIndex_DoesNotExposeUserRealmIdInTable()
        {
            // Seed one note for the test user directly via EF (bypasses CSRF, faster setup).
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<NoteKeeperContext>();
                ctx.Notes.Add(new Note
                {
                    Summary = "Visible Summary",
                    Details = "Visible Details",
                    UserRealmId = UserId,
                    CreatedDateUtc = DateTimeOffset.UtcNow,
                    ModifiedDateUtc = DateTimeOffset.UtcNow,
                });
                await ctx.SaveChangesAsync();
            }

            using var client = CreateAuthenticatedClient();
            var response = await client.GetAsync("/Notes/Index");
            var html = await response.Content.ReadAsStringAsync();

            // Confirm the note's user-facing content IS rendered (5.1.3 companion assertion).
            var tbodyHtml = ExtractTbodyHtml(html);
            tbodyHtml.Should().Contain("Visible Summary",
                because: "the note's Summary should be visible in the notes list");

            // The UserRealmId (Entra object identifier) must NOT appear inside the table body.
            tbodyHtml.Should().NotContain(UserId,
                because: "requirement 5.1.4 prohibits displaying internal multi-tenant " +
                         "enforcement data (UserRealmId) in the note list");
        }
    }
}
