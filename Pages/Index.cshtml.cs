using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Net;
using Microsoft.Graph;

namespace HW5NoteKeeperSolution.Pages
{
    [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
    public class IndexModel : PageModel
    {
        private readonly GraphServiceClient _graphServiceClient;
        public async Task OnGet()
        {
            var user = await _graphServiceClient.Me.Request().GetAsync();;
            ViewData["GraphApiResult"] = user.DisplayName;;

        }
    }
}
