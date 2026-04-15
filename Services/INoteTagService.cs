using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Services
{
    public interface INoteTagService
    {
        Task ApplyGeneratedTagsAsync(Note note, bool replaceExistingTags = true, CancellationToken cancellationToken = default);
    }
}
