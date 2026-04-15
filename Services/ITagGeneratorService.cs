using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Services
{
    public interface ITagGeneratorService
    {
        Task<KeyTagsResponse> GenerateTagsAsync(string details, CancellationToken cancellationToken = default);
    }
}
