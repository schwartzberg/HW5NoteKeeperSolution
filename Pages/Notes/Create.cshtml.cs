using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;
using Microsoft.AspNetCore.Mvc;

namespace HW5NoteKeeper.Pages.Notes
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
        /// Handles POST requests. Validates the model, generates AI tags, creates <c>Tag</c>
        /// entities via <c>Context.Tags.Add</c>, persists everything with a single save, and
        /// redirects to the Index.
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

            KeyTagsResponse tagResponse = await _noteTagService.ApplyGeneratedTags(Note.Details);

            Context.Notes.Add(Note);

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
                        NoteId = Note.Id,
                        Name = tagName
                    });
                }
            }

            await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
