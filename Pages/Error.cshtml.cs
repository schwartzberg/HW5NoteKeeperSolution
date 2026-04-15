using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HW5NoteKeeperSolution.Pages
{
    /// <summary>Razor Page model for the application error page. Captures the current request ID for display.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        /// <summary>Gets or sets the current HTTP request ID (from Activity or TraceIdentifier).</summary>
        public string? RequestId { get; set; }

        /// <summary>Returns <see langword="true"/> when <see cref="RequestId"/> is non-empty and should be shown to the user.</summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// <summary>Handles GET requests. Populates <see cref="RequestId"/> from the current diagnostic activity.</summary>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }

}
