using System.Security.Claims;
using System.Security.Principal;

namespace HW5NoteKeeper;

/// <summary>
/// Provides helper extension methods for working with claims-based identities in the Razor Pages app.
/// </summary>
/// <remarks>
/// These helpers centralize claim access so Razor views and application code can read identity data by
/// intent instead of repeating raw claim-type lookups. This keeps UI code simpler and makes it easier to
/// update claim-resolution behavior in a single place if the authentication configuration changes.
/// </remarks>
public static class ClaimsHelpers
{
    /// <summary>
    /// Gets the value of the <c>name</c> claim from the supplied identity.
    /// </summary>
    /// <param name="identity">
    /// The identity to inspect. This may be <see langword="null"/>, unauthenticated, or any implementation of
    /// <see cref="IIdentity"/>.
    /// </param>
    /// <returns>
    /// The value of the <c>name</c> claim when <paramref name="identity"/> is a <see cref="ClaimsIdentity"/> and
    /// that claim is present; otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method reads the literal <c>name</c> claim instead of <see cref="IIdentity.Name"/>. That distinction is
    /// useful when the identity name has not been mapped by the authentication system, but the underlying claims
    /// collection still contains the display name provided by the identity provider.
    /// </remarks>
    public static string? ClaimName(this IIdentity? identity)
    {
        if (identity is not ClaimsIdentity claimsIdentity)
        {
            return null;
        }

        // Check both long-form (mapped) and short-form (unmapped) claim types
        // because MapInboundClaims = false keeps the original OIDC token names.
        string? firstName = claimsIdentity.FindFirst( ClaimTypes.GivenName)?.Value
            ?? claimsIdentity.FindFirst("given_name")?.Value;
        string? lastName = claimsIdentity.FindFirst(ClaimTypes.Surname)?.Value
            ?? claimsIdentity.FindFirst("family_name")?.Value;

        if (firstName == null && lastName == null)
        {
            return claimsIdentity.FindFirst("name")?.Value;
        }

        return $"{firstName} {lastName}".Trim();
    }
}
