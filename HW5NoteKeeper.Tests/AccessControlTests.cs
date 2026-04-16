using FluentAssertions;
using HW5NoteKeeper.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Tests requirement 3.1: only authenticated users can access Notes CRUD pages.
    /// Unauthenticated requests must be blocked with <see cref="HttpStatusCode.Unauthorized"/>.
    /// The <see cref="NoteKeeperWebApplicationFactory"/> uses <see cref="TestAuthHandler"/>,
    /// which returns 401 when no <c>X-Test-User-Id</c> header is present — mirroring the
    /// production OIDC challenge redirect.
    /// </summary>
    public class AccessControlTests : IClassFixture<NoteKeeperWebApplicationFactory>
    {
        private readonly NoteKeeperWebApplicationFactory _factory;

        /// <summary>
        /// Initializes a new instance of <see cref="AccessControlTests"/>.
        /// </summary>
        /// <param name="factory">Shared <see cref="NoteKeeperWebApplicationFactory"/> instance.</param>
        public AccessControlTests(NoteKeeperWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateUnauthenticatedClient() =>
            _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

        /// <summary>
        /// Requirement 3.1: GET requests from unauthenticated users to any Notes CRUD page
        /// must return <see cref="HttpStatusCode.Unauthorized"/>.
        /// </summary>
        [Theory]
        [InlineData("/Notes")]
        [InlineData("/Notes/Index")]
        [InlineData("/Notes/Create")]
        [InlineData("/Notes/Details?id=00000000-0000-0000-0000-000000000001")]
        [InlineData("/Notes/Edit?id=00000000-0000-0000-0000-000000000001")]
        [InlineData("/Notes/Delete?id=00000000-0000-0000-0000-000000000001")]
        public async Task UnauthenticatedUser_NotesCrudPage_ReturnsUnauthorized(string path)
        {
            using var client = CreateUnauthenticatedClient();

            var response = await client.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                because: $"unauthenticated GET {path} must be blocked (requirement 3.1)");
        }

        /// <summary>
        /// Positive confirmation: an authenticated user can access the Notes index page.
        /// </summary>
        [Fact]
        public async Task AuthenticatedUser_NotesIndex_ReturnsOk()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "access-control-test-user");

            var response = await client.GetAsync("/Notes");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "an authenticated user must be able to reach the Notes index (requirement 3.1)");
        }

        // ── Error page anonymous access ──────────────────────────────────────────

        /// <summary>
        /// The Error page must be accessible without authentication so that errors
        /// (including auth-related failures) can render cleanly without triggering
        /// a redirect loop through the OIDC challenge flow.
        /// </summary>
        [Fact]
        public async Task UnauthenticatedUser_ErrorPage_DoesNotRequireAuth()
        {
            using var client = CreateUnauthenticatedClient();

            var response = await client.GetAsync("/Error");

            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
                because: "the Error page is AllowAnonymous so error screens can render " +
                         "without triggering an auth challenge redirect loop");
        }

        /// <summary>
        /// The Privacy page, unlike the Error page, requires authentication.
        /// </summary>
        [Fact]
        public async Task UnauthenticatedUser_PrivacyPage_ReturnsUnauthorized()
        {
            using var client = CreateUnauthenticatedClient();

            var response = await client.GetAsync("/Privacy");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                because: "Privacy is not in the AllowAnonymous list and the FallbackPolicy " +
                         "plus AuthorizeFolder(\"/\") require authentication");
        }
    }
}
