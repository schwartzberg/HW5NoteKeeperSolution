using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using HW5NoteKeeperSolution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    /// <summary>
    /// Razor Page model for editing an existing <see cref="Note"/>.
    /// Regenerates AI tags when the note's <c>Details</c> text changes.
    /// Enforces multi-tenant ownership on both GET and POST.
    /// </summary>
    public class EditModel : NoteKeeperBasePageModel
    {
        private readonly INoteTagService _noteTagService;

        /// <summary>
        /// Initializes a new instance of <see cref="EditModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        /// <param name="noteTagService">Service that regenerates AI keyword tags when details change.</param>
        public EditModel(NoteKeeperContext context, INoteTagService noteTagService) : base(context)
        {
            _noteTagService = noteTagService;
        }

        /// <summary>Gets or sets the note bound from the Edit form.</summary>
        [BindProperty]
        public Note Note { get; set; } = default!;

        /// <summary>
        /// Handles GET requests. Loads the note with its tags for editing.
        /// Returns 404 when the note does not exist or belongs to another user.
        /// </summary>
        /// <param name="id">The unique identifier of the note to edit.</param>
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
        /// Handles POST requests. Validates ownership, updates the note, optionally regenerates tags
        /// when <c>Details</c> changed, and redirects to the Index on success.
        /// </summary>
        /// <returns>The page on validation or ownership error; a redirect to <c>./Index</c> on success.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Guard: the UserRealmId posted back must match the authenticated user.
            if (Note.UserRealmId != User.GetObjectIdentifier())
            {
                ModelState.AddModelError("realm", "This note cannot be updated because the user does not have access to it.");
                return Page();
            }

            // Re-load from DB so we can compare Details and manage tags correctly.
            var existingNote = await Context.Notes
                .Include(n => n.Tags)
                .FirstOrDefaultAsync(n => n.Id == Note.Id && n.UserRealmId == User.GetObjectIdentifier());

            if (existingNote == null)
            {
                return NotFound();
            }

            bool detailsChanged = !string.Equals(existingNote.Details, Note.Details, StringComparison.Ordinal);

            if (detailsChanged)
            {
                // Save tag deletion separately so the DELETE batch does not
                // conflict with the subsequent note UPDATE + tag INSERT batch.
                existingNote.Tags.Clear();
                await Context.SaveChangesAsync();

                // Clear the change tracker and reload so the second save starts
                // with a clean entity state — avoids DbUpdateConcurrencyException.
                Context.ChangeTracker.Clear();
                existingNote = await Context.Notes
                    .Include(n => n.Tags)
                    .FirstOrDefaultAsync(n => n.Id == Note.Id && n.UserRealmId == User.GetObjectIdentifier());

                if (existingNote == null)
                {
                    return NotFound();
                }
            }

            existingNote.Summary = Note.Summary;
            existingNote.Details = Note.Details;
            existingNote.ModifiedDateUtc = DateTimeOffset.UtcNow;
             
            if (detailsChanged)
            {
                await _noteTagService.ApplyGeneratedTagsAsync(existingNote, replaceExistingTags: false);
            }

            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NoteExists(Note.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool NoteExists(Guid id)
        {
            return Context.Notes.Any(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());
        }
    }
}
