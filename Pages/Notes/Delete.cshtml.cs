using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    /// <summary>
    /// Razor Page model for the Delete confirmation page.
    /// Only allows deletion of notes that belong to the authenticated user.
    /// </summary>
    public class DeleteModel : NoteKeeperBasePageModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DeleteModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        public DeleteModel(NoteKeeperContext context) : base(context) { }

        /// <summary>Gets or sets the note to be deleted, bound from the form.</summary>
        [BindProperty]
        public Note Note { get; set; } = default!;

        /// <summary>
        /// Handles GET requests. Loads the note for confirmation, enforcing multi-tenant ownership.
        /// Returns 404 when the note is not found or belongs to another user.
        /// </summary>
        /// <param name="id">The unique identifier of the note to delete.</param>
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

        /// <summary>
        /// Handles POST requests. Deletes the note (and its tags via cascade) if it belongs to the current user,
        /// then redirects to the Index. Silently skips deletion if the note is not found or belongs to another user.
        /// </summary>
        /// <param name="id">The unique identifier of the note to delete (from the query string).</param>
        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Only delete if the note belongs to the current user (tags cascade via EF).
            var note = await Context.Notes
                .Include(n => n.Tags)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());

            if (note != null)
            {
                Context.Notes.Remove(note);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
