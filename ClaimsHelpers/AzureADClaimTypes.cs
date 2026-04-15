using System.Security.Claims;

namespace HW5NoteKeeperSolution
{
    /// <summary>
    /// Defines the claim types exposed by this app's authenticated <see cref="ClaimsPrincipal"/>
    /// when signing in with Microsoft Entra External ID through Microsoft.Identity.Web.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core and the OpenID Connect handler can map some incoming token claims to the
    /// long-form claim URIs on the resulting principal. These constants reflect the claim types
    /// actually used by this application at runtime.
    /// See <see href="https://learn.microsoft.com/aspnet/core/security/authentication/claims?view=aspnetcore-10.0">Mapping, customizing, and transforming claims in ASP.NET Core</see>.
    /// </remarks>
    public static class AzureADClaimTypes
    {
        /// <summary>
        /// Tenant ID for the current Microsoft Entra tenant.
        /// </summary>
        public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// Immutable object identifier for the signed-in user in the current tenant.
        /// </summary>
        public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        /// <summary>
        /// Preferred username for the signed-in user.
        /// </summary>
        public const string PreferredUsername = "preferred_username";

        /// <summary>
        /// Email address claim on the mapped authenticated principal.
        /// </summary>
        public const string EmailAddress = ClaimTypes.Email;

        /// <summary>
        /// Human-readable display name for the signed-in user.
        /// </summary>
        public const string Name = "name";

        /// <summary>
        /// Subject identifier for the signed-in user on the mapped authenticated principal.
        /// </summary>
        public const string Subject = ClaimTypes.NameIdentifier;
    }
}
