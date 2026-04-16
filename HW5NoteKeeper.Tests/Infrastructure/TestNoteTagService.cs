using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;

namespace HW5NoteKeeper.Tests.Infrastructure
{
    /// <summary>
    /// Test implementation of <see cref="INoteTagService"/> that returns a deterministic
    /// <see cref="KeyTagsResponse"/> without calling Azure OpenAI. Callers are responsible
    /// for creating <c>Tag</c> entities from the response, matching the production pattern.
    /// </summary>
    internal sealed class TestNoteTagService : INoteTagService
    {
        public Task<KeyTagsResponse> ApplyGeneratedTags(
            string details,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new KeyTagsResponse
            {
                Tags = new List<string> { "generated-tag" }
            });
        }
    }
}
