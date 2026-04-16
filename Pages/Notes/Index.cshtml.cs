using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeper.Pages.Notes
{
    /// <summary>
    /// Razor Page model for the Notes list (Index) page.
    /// Displays only the notes that belong to the authenticated user, ordered by most recently created.
    /// Seeds default notes on first visit when the user has none.
    /// </summary>
    public class IndexModel : NoteKeeperBasePageModel
    {
        private readonly IUserNoteSeedService _seedService;

        /// <summary>
        /// Initializes a new instance of <see cref="IndexModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        /// <param name="seedService">Service that seeds default notes for first-time users.</param>
        public IndexModel(NoteKeeperContext context, IUserNoteSeedService seedService) : base(context)
        {
            _seedService = seedService;
        }

        /// <summary>Gets or sets the list of notes owned by the current user.</summary>
        public IList<Note> Note { get; set; } = default!;

        /// <summary>
        /// Handles GET requests. Seeds default notes if the user has none, then loads all
        /// notes (with tags) belonging to the authenticated user, ordered by descending creation date.
        /// </summary>
        public async Task OnGetAsync()
        {
            var userRealmId = User.GetObjectIdentifier();

            await _seedService.EnsureSeedDataAsync(userRealmId, HttpContext.RequestAborted);

            Note = await Context.Notes
                .Include(n => n.Tags)
                .Where(n => n.UserRealmId == userRealmId)
                .OrderByDescending(n => n.CreatedDateUtc)
                .ToListAsync();
        }
    }
}
