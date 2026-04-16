using System.Security.Claims;

namespace HW5NoteKeeper
{
    /// <summary>
    /// Legacy extension methods for reading Microsoft Entra claims from a <see cref="ClaimsPrincipal"/>.
    /// Prefer the methods in <see cref="HW5NoteKeeper.ClaimsPrincipalExtensions"/> which use
    /// the strongly-typed <see cref="AzureADClaimTypes"/> constants.
    /// </summary>
    public static class ClaimsPrincipalExtensions1
    {
        /// <summary>
        /// Retrieves the object identifier claim using the raw schema URI.
        /// </summary>
        /// <param name="cp">The claims principal.</param>
        /// <returns>The object identifier value, or an empty string if the claim is absent.</returns>
        public static string ObjectIdentifier(this ClaimsPrincipal cp)
        {
            return cp.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
        }
    }
}
