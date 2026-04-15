using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using HW5NoteKeeperSolution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    public class EditModel : NoteKeeperBasePageModel
    {
        private readonly INoteTagService _noteTagService;

        public EditModel(NoteKeeperContext context, INoteTagService noteTagService) : base(context)
        {
            _noteTagService = noteTagService;
        }

        [BindProperty]
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

            existingNote.Summary = Note.Summary;
            existingNote.Details = Note.Details;
            existingNote.ModifiedDateUtc = DateTimeOffset.UtcNow;

            if (detailsChanged)
            {
                // Explicitly remove old tags via DbContext before regenerating,
                // then call the service to generate and attach new tags.
                var tagsToRemove = existingNote.Tags.ToList();
                if (tagsToRemove.Any())
                {
                    Context.Tags.RemoveRange(tagsToRemove);
                    existingNote.Tags.Clear();
                }
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
