using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    /// <summary>
    /// Razor Page model for the Notes list (Index) page.
    /// Displays only the notes that belong to the authenticated user, ordered by most recently created.
    /// </summary>
    public class IndexModel : NoteKeeperBasePageModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IndexModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        public IndexModel(NoteKeeperContext context) : base(context) { }

        /// <summary>Gets or sets the list of notes owned by the current user.</summary>
        public IList<Note> Note { get; set; } = default!;

        /// <summary>
        /// Handles GET requests. Loads all notes (with tags) belonging to the authenticated user,
        /// ordered by descending creation date.
        /// </summary>
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
