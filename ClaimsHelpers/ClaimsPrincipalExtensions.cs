using System.Security.Claims;

namespace HW5NoteKeeperSolution
{
    /// <summary>
    /// Extension methods for reading Microsoft Entra claims from a <see cref="ClaimsPrincipal"/>
    /// using the strongly-typed <see cref="AzureADClaimTypes"/> constants.
    /// </summary>
    // Helper methods to read the claims exposed on the authenticated principal.
    public static class ClaimsPrincipalExtensions
    {

        /// <summary>
        /// Retrieves the immutable object identifier for the user in Microsoft Entra.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns>The object identifier for the user in Microsoft Entra.</returns>
        public static string GetObjectIdentifier(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst(AzureADClaimTypes.ObjectId)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Retrieves a human readable display name of the user.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal to inspect.</param>
        /// <returns>A human readable display name of the user.</returns>
        public static string GetDisplayName(this ClaimsPrincipal claimsPrincipal)
        {
            string returnedValue = claimsPrincipal.FindFirst(AzureADClaimTypes.Name)?.Value ?? string.Empty;

            return returnedValue;
        }

        /// <summary>
        /// Retrieves the email address of the user.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal to inspect.</param>
        /// <returns>The email address of the user.</returns>
        public static string GetEmailAddress(this ClaimsPrincipal claimsPrincipal)
        {
            // Check long-form (mapped) and short-form (unmapped) claim types
            // because MapInboundClaims = false keeps the original OIDC token names.
            return claimsPrincipal.FindFirst(AzureADClaimTypes.EmailAddress)?.Value
                ?? claimsPrincipal.FindFirst("email")?.Value
                ?? claimsPrincipal.FindFirst("emails")?.Value
                ?? claimsPrincipal.FindFirst("preferred_username")?.Value
                ?? string.Empty;
        }

        /// <summary>
        /// Retrieves the preferred username for the user.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal to inspect.</param>
        /// <returns>The preferred username.</returns>
        public static string GetUserPrincipalName(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst(AzureADClaimTypes.PreferredUsername)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Retrieves the tenant ID.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal to inspect.</param>
        /// <returns>The tenant ID.</returns>
        public static string GetTenantId(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst(AzureADClaimTypes.TenantId)?.Value ?? string.Empty;
        }
    }
}
