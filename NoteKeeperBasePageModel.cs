using HW5NoteKeeperSolution.Data;

namespace HW5NoteKeeperSolution.Pages
{
    public class NoteKeeperBasePageModel : BasePageModel
    {
        protected NoteKeeperContext Context { get; }

        public NoteKeeperBasePageModel(NoteKeeperContext context)
        {
            Context = context;
        }
    }
}
