using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Services
{
    /// <summary>
    /// Defines the contract for generating and attaching AI-produced keyword tags to a <see cref="Note"/>.
    /// </summary>
    public interface INoteTagService
    {
        /// <summary>
        /// Generates keyword tags for <paramref name="note"/> using the configured AI model and attaches them
        /// to the note's <c>Tags</c> collection.
        /// </summary>
        /// <param name="note">The note to tag. Its <c>Details</c> text is sent to the AI model.</param>
        /// <param name="replaceExistingTags">
        /// When <see langword="true"/> (default) the existing tags are cleared before new ones are added.
        /// Pass <see langword="false"/> on edit when old tags have already been removed via <c>DbContext</c>.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task ApplyGeneratedTagsAsync(Note note, bool replaceExistingTags = true, CancellationToken cancellationToken = default);
    }
}
