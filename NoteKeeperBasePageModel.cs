using HW5NoteKeeper.Data;

namespace HW5NoteKeeper.Pages
{
    /// <summary>
    /// Base Razor Page model that provides access to the <see cref="NoteKeeperContext"/> EF Core database context.
    /// All Notes page models inherit from this class.
    /// </summary>
    public class NoteKeeperBasePageModel : BasePageModel
    {
        /// <summary>Gets the EF Core database context for this page.</summary>
        protected NoteKeeperContext Context { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="NoteKeeperBasePageModel"/> with the given database context.
        /// </summary>
        /// <param name="context">The <see cref="NoteKeeperContext"/> to use for database access.</param>
        public NoteKeeperBasePageModel(NoteKeeperContext context)
        {
            Context = context;
        }
    }
}
