using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HW5NoteKeeperSolution.Pages
{
    /// <summary>
    /// Razor Page model for the application home page.
    /// Unauthenticated users see the welcome screen; authenticated users are immediately
    /// redirected to the Notes list so they can manage their notes.
    /// </summary>
    public class IndexModel : PageModel
    {
        /// <summary>
        /// Handles GET requests.
        /// Authenticated principals are redirected to <c>/Notes/Index</c>.
        /// Anonymous visitors receive the welcome page.
        /// </summary>
        /// <returns>
        /// A <see cref="RedirectToPageResult"/> to <c>/Notes/Index</c> for authenticated users,
        /// or <see cref="PageResult"/> for anonymous visitors.
        /// </returns>
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Notes/Index");

            return Page();
        }
    }
}



