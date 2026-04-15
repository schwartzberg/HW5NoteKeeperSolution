using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    public class IndexModel : NoteKeeperBasePageModel
    {
        public IndexModel(NoteKeeperContext context) : base(context) { }

        public IList<Note> Note { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Note = await Context.Notes
                .Include(n => n.Tags)
                .Where(n => n.UserRealmId == User.GetObjectIdentifier())
                .OrderByDescending(n => n.CreatedDateUtc)
                .ToListAsync();
        }
    }
}
