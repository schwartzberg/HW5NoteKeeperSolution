using System.Security.Claims;

namespace HW5NoteKeeperSolution
{
    public static class ClaimsPrincipalExtensions1
    {
        public static string ObjectIdentifier(this ClaimsPrincipal cp)
        {
            return cp.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
        }
    } 
}
