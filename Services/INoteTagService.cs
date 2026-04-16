using HW5NoteKeeper.Models;

namespace HW5NoteKeeper.Services
{
    /// <summary>
    /// Defines the contract for generating keyword tags from note detail text
    /// using the configured AI model. Returns a <see cref="KeyTagsResponse"/>
    /// with raw tag strings — callers are responsible for creating <c>Tag</c>
    /// entities and attaching them to notes.
    /// </summary>
    public interface INoteTagService
    {
        /// <summary>
        /// Generates keyword tags from <paramref name="details"/> using Azure OpenAI.
        /// Includes retry logic with exponential backoff and rate-limit (429) handling.
        /// </summary>
        /// <param name="details">The note detail text to summarize as keywords.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="KeyTagsResponse"/> with the generated tag list.</returns>
        Task<KeyTagsResponse> ApplyGeneratedTags(string details, CancellationToken cancellationToken = default);
    }
}
