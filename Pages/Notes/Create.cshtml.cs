using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using HW5NoteKeeperSolution.Services;
using Microsoft.AspNetCore.Mvc;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    /// <summary>
    /// Razor Page model for creating a new <see cref="Note"/>.
    /// Generates AI tags and assigns the authenticated user's Entra object ID as <c>UserRealmId</c>.
    /// </summary>
    public class CreateModel : NoteKeeperBasePageModel
    {
        private readonly INoteTagService _noteTagService;

        /// <summary>
        /// Initializes a new instance of <see cref="CreateModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        /// <param name="noteTagService">Service that generates and attaches AI keyword tags.</param>
        public CreateModel(NoteKeeperContext context, INoteTagService noteTagService) : base(context)
        {
            _noteTagService = noteTagService;
        }

        /// <summary>Handles GET requests. Returns the Create form page.</summary>
        public IActionResult OnGet() => Page();

        /// <summary>Gets or sets the new note bound from the Create form submission.</summary>
        [BindProperty]
        public Note Note { get; set; } = default!;

        /// <summary>
        /// Handles POST requests. Validates the model, generates AI tags, persists the note, and redirects to the Index.
        /// </summary>
        /// <returns>The page on validation error; a redirect to <c>./Index</c> on success.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Note.Id = Guid.NewGuid();
            Note.UserRealmId = User.GetObjectIdentifier();
            Note.CreatedDateUtc = DateTimeOffset.UtcNow;
            Note.ModifiedDateUtc = null;

            await _noteTagService.ApplyGeneratedTagsAsync(Note);
            Context.Notes.Add(Note);
            await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
