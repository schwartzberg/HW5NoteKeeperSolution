using FluentAssertions;
using System.Security.Claims;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ClaimsPrincipalExtensions"/> and <see cref="AzureADClaimTypes"/>.
    /// </summary>
    public class ClaimsPrincipalExtensionsTests
    {
        private static ClaimsPrincipal MakePrincipal(params Claim[] claims)
        {
            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        // ------- GetObjectIdentifier -------

        [Fact]
        public void GetObjectIdentifier_WithOidClaim_ReturnsValue()
        {
            string expected = Guid.NewGuid().ToString();
            ClaimsPrincipal principal = MakePrincipal(
                new Claim(AzureADClaimTypes.ObjectId, expected));

            principal.GetObjectIdentifier().Should().Be(expected);
        }

        [Fact]
        public void GetObjectIdentifier_WithoutOidClaim_ReturnsEmptyString()
        {
            ClaimsPrincipal principal = MakePrincipal();

            principal.GetObjectIdentifier().Should().BeEmpty();
        }

        // ------- GetDisplayName -------

        [Fact]
        public void GetDisplayName_WithNameClaim_ReturnsValue()
        {
            ClaimsPrincipal principal = MakePrincipal(
                new Claim(AzureADClaimTypes.Name, "Jane Doe"));

            principal.GetDisplayName().Should().Be("Jane Doe");
        }

        [Fact]
        public void GetDisplayName_WithoutNameClaim_ReturnsEmptyString()
        {
            ClaimsPrincipal principal = MakePrincipal();

            principal.GetDisplayName().Should().BeEmpty();
        }

        // ------- GetEmailAddress -------

        [Fact]
        public void GetEmailAddress_WithEmailClaim_ReturnsValue()
        {
            ClaimsPrincipal principal = MakePrincipal(
                new Claim(AzureADClaimTypes.EmailAddress, "jane@example.com"));

            principal.GetEmailAddress().Should().Be("jane@example.com");
        }

        [Fact]
        public void GetEmailAddress_WithoutEmailClaim_ReturnsEmptyString()
        {
            ClaimsPrincipal principal = MakePrincipal();

            principal.GetEmailAddress().Should().BeEmpty();
        }

        [Fact]
        public void GetEmailAddress_WithShortFormEmailClaim_ReturnsValue()
        {
            ClaimsPrincipal principal = MakePrincipal(
                new Claim("email", "short@example.com"));

            principal.GetEmailAddress().Should().Be("short@example.com");
        }

        [Fact]
        public void GetEmailAddress_WithShortFormEmailsClaim_ReturnsValue()
        {
            ClaimsPrincipal principal = MakePrincipal(
                new Claim("emails", "multi@example.com"));

            principal.GetEmailAddress().Should().Be("multi@example.com");
        }

        [Fact]
        public void GetEmailAddress_WithPreferredUsernameFallback_ReturnsValue()
        {
            ClaimsPrincipal principal = MakePrincipal(
                new Claim("preferred_username", "ciam-user@example.com"));

            principal.GetEmailAddress().Should().Be("ciam-user@example.com");
        }

        // ------- GetUserPrincipalName -------

        [Fact]
        public void GetUserPrincipalName_WithPreferredUsernameClaim_ReturnsValue()
        {
            ClaimsPrincipal principal = MakePrincipal(
                new Claim(AzureADClaimTypes.PreferredUsername, "jane"));

            principal.GetUserPrincipalName().Should().Be("jane");
        }

        [Fact]
        public void GetUserPrincipalName_WithoutClaim_ReturnsEmptyString()
        {
            ClaimsPrincipal principal = MakePrincipal();

            principal.GetUserPrincipalName().Should().BeEmpty();
        }

        // ------- GetTenantId -------

        [Fact]
        public void GetTenantId_WithTenantIdClaim_ReturnsValue()
        {
            string tenantId = Guid.NewGuid().ToString();
            ClaimsPrincipal principal = MakePrincipal(
                new Claim(AzureADClaimTypes.TenantId, tenantId));

            principal.GetTenantId().Should().Be(tenantId);
        }

        [Fact]
        public void GetTenantId_WithoutClaim_ReturnsEmptyString()
        {
            ClaimsPrincipal principal = MakePrincipal();

            principal.GetTenantId().Should().BeEmpty();
        }

        // ------- Multiple claims on same principal -------

        [Fact]
        public void AllGetters_MultipleClaims_ReturnCorrectValues()
        {
            string oid = Guid.NewGuid().ToString();
            string tid = Guid.NewGuid().ToString();

            ClaimsPrincipal principal = MakePrincipal(
                new Claim(AzureADClaimTypes.ObjectId, oid),
                new Claim(AzureADClaimTypes.TenantId, tid),
                new Claim(AzureADClaimTypes.Name, "Alice"),
                new Claim(AzureADClaimTypes.EmailAddress, "alice@example.com"),
                new Claim(AzureADClaimTypes.PreferredUsername, "alice.user"));

            principal.GetObjectIdentifier().Should().Be(oid);
            principal.GetTenantId().Should().Be(tid);
            principal.GetDisplayName().Should().Be("Alice");
            principal.GetEmailAddress().Should().Be("alice@example.com");
            principal.GetUserPrincipalName().Should().Be("alice.user");
        }

        // ------- ClaimName (ClaimsHelpers) — short-form claims -------

        [Fact]
        public void ClaimName_WithShortFormGivenNameAndFamilyName_ReturnsCombined()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("given_name", "Joe"),
                new Claim("family_name", "Ficara"),
            }, "TestAuth");

            identity.ClaimName().Should().Be("Joe Ficara");
        }

        [Fact]
        public void ClaimName_WithoutGivenOrFamilyName_FallsBackToNameClaim()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("name", "DisplayOnly"),
            }, "TestAuth");

            identity.ClaimName().Should().Be("DisplayOnly");
        }

        [Fact]
        public void ClaimName_NullIdentity_ReturnsNull()
        {
            ((System.Security.Principal.IIdentity?)null).ClaimName().Should().BeNull();
        }
    }
}
