using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using HW5NoteKeeperSolution.Services;
using Microsoft.AspNetCore.Mvc;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    public class CreateModel : NoteKeeperBasePageModel
    {
        private readonly INoteTagService _noteTagService;

        public CreateModel(NoteKeeperContext context, INoteTagService noteTagService) : base(context)
        {
            _noteTagService = noteTagService;
        }

        public IActionResult OnGet() => Page();

        [BindProperty]
        public Note Note { get; set; } = default!;

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
