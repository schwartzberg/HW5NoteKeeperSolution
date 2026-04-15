using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    /// <summary>
    /// Razor Page model for the Note Details (read-only view) page.
    /// Enforces multi-tenant ownership — returns 404 for notes belonging to other users.
    /// </summary>
    public class DetailsModel : NoteKeeperBasePageModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DetailsModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        public DetailsModel(NoteKeeperContext context) : base(context) { }

        /// <summary>Gets or sets the note to display.</summary>
        public Note Note { get; set; } = default!;

        /// <summary>
        /// Handles GET requests. Loads the note with its tags, enforcing multi-tenant ownership.
        /// Returns 404 when the note does not exist or belongs to another user.
        /// </summary>
        /// <param name="id">The unique identifier of the note to display.</param>
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
