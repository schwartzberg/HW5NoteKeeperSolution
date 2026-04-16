namespace HW5NoteKeeper.Data
{
    /// <summary>
    /// Defines the contract for initialising Azure Blob Storage containers and seeding
    /// per-note attachment files during first-login data seeding.
    /// </summary>
    public interface IAzureStorageInitializer
    {
        /// <summary>
        /// Gets the mapping from note summary text to the list of seed attachment file names
        /// that should be uploaded to the note's blob container.
        /// </summary>
        IReadOnlyDictionary<string, string[]> AttachmentMapping { get; }

        /// <summary>
        /// Creates the blob container for the given note and uploads its seed attachments.
        /// Returns <see langword="true"/> when seeding is successful or no attachments are mapped for the summary.
        /// </summary>
        /// <param name="noteId">The unique identifier of the note (used as the container name).</param>
        /// <param name="summary">The note summary used to look up the attachment mapping.</param>
        /// <param name="attachmentsDirectory">
        /// Optional path to the directory containing the seed attachment files.
        /// Defaults to <c>AzureStorageAttachments</c> under the application base directory.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> if the operation completed successfully.</returns>
        Task<bool> InitializeAsync(
            Guid noteId,
            string summary,
            string? attachmentsDirectory = null,
            CancellationToken cancellationToken = default);
    }
}
