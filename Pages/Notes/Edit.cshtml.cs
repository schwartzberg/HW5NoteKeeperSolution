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
        /// Handles POST requests. Validates ownership, updates the note, regenerates tags
        /// when <c>Details</c> changed (using <c>RemoveRange</c> / <c>Tags.Add</c> to avoid
        /// EF concurrency conflicts), and performs a single <c>SaveChangesAsync</c>.
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

            // Re-load from DB so we can compare fields and manage tags correctly.
            var existingNote = await Context.Notes
                .Include(n => n.Tags)
                .FirstOrDefaultAsync(n => n.Id == Note.Id && n.UserRealmId == User.GetObjectIdentifier());

            if (existingNote == null)
            {
                return NotFound();
            }

            bool summaryChanged = !string.Equals(existingNote.Summary, Note.Summary, StringComparison.Ordinal);
            bool detailsChanged = !string.Equals(existingNote.Details?.Trim(), Note.Details?.Trim(), StringComparison.Ordinal);

            if (summaryChanged)
            {
                existingNote.Summary = Note.Summary;
            }

            if (detailsChanged)
            {
                existingNote.Details = Note.Details;

                // Generate new tags from AI
                KeyTagsResponse tagResponse = await _noteTagService.ApplyGeneratedTags(existingNote.Details);

                // Remove old tags via DbContext (avoids EF tracking conflicts)
                Context.Tags.RemoveRange(existingNote.Tags);

                // Add new tags with normalization
                if (tagResponse.Tags != null && tagResponse.Tags.Count > 0)
                {
                    foreach (string tagName in tagResponse.Tags
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => t.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(t => t.Length > 30 ? t[..30] : t)
                        .Take(5))
                    {
                        Context.Tags.Add(new Tag
                        {
                            Id = Guid.NewGuid(),
                            NoteId = existingNote.Id,
                            Name = tagName
                        });
                    }
                }
            }

            if (summaryChanged || detailsChanged)
            {
                existingNote.ModifiedDateUtc = DateTimeOffset.UtcNow;

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
            }

            return RedirectToPage("./Index");
        }

        private bool NoteExists(Guid id)
        {
            return Context.Notes.Any(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());
        }
    }
}
