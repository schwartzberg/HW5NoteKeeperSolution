using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    public class DetailsModel : NoteKeeperBasePageModel
    {
        public DetailsModel(NoteKeeperContext context) : base(context) { }

        public Note Note { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await Context.Notes
                .Include(n => n.Tags)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());

            if (note == null)
            {
                return NotFound();
            }

            Note = note;
            return Page();
        }
    }
}
