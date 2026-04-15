namespace HW5NoteKeeperSolution.Data
{
    public interface IAzureStorageInitializer
    {
        IReadOnlyDictionary<string, string[]> AttachmentMapping { get; }

        Task<bool> InitializeAsync(
            Guid noteId,
            string summary,
            string? attachmentsDirectory = null,
            CancellationToken cancellationToken = default);
    }
}
