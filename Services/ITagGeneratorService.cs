using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Services
{
    /// <summary>
    /// Defines the contract for calling the Azure OpenAI API to generate keyword tags
    /// from a note's detail text.
    /// </summary>
    public interface ITagGeneratorService
    {
        /// <summary>
        /// Sends <paramref name="details"/> to the configured Azure OpenAI deployment and returns
        /// a <see cref="KeyTagsResponse"/> containing 3–5 keyword tags.
        /// </summary>
        /// <param name="details">The note detail text to summarize as keywords.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="KeyTagsResponse"/> with the generated tag list.</returns>
        Task<KeyTagsResponse> GenerateTagsAsync(string details, CancellationToken cancellationToken = default);
    }
}
