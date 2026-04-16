namespace HW5NoteKeeper.Services
{
    /// <summary>
    /// Result of an attachment deletion operation.
    /// </summary>
    public enum AttachmentDeleteResult
    {
        /// <summary>The blob was found and deleted successfully.</summary>
        Deleted,
        /// <summary>The blob was not present in storage.</summary>
        NotFound,
        /// <summary>The blob was present but could not be deleted due to an error.</summary>
        Error
    }

    /// <summary>
    /// Defines the contract for Azure Blob Storage attachment operations:
    /// upload, download, delete, list, and count.
    /// </summary>
    public interface IAzureStorageService
    {
        /// <summary>
        /// Uploads a file as a blob to the container associated with the given note.
        /// Creates the container if it does not already exist.
        /// Stores the original file name and note ID as blob metadata.
        /// </summary>
        /// <param name="noteId">The note's ID (used as the container name).</param>
        /// <param name="attachmentId">The blob name (GUID-based).</param>
        /// <param name="fileData">The file to upload.</param>
        /// <returns><c>true</c> if the blob was newly created; <c>false</c> if it was updated.</returns>
        Task<bool> UploadAttachmentAsync(string noteId, string attachmentId, IFormFile fileData);

        /// <summary>
        /// Deletes the blob with the specified attachment ID from the container associated with the given note.
        /// </summary>
        /// <param name="noteId">The note's ID (container name).</param>
        /// <param name="attachmentId">The blob name to delete.</param>
        /// <returns>An <see cref="AttachmentDeleteResult"/> indicating the outcome.</returns>
        Task<AttachmentDeleteResult> DeleteAttachmentAsync(string noteId, string attachmentId);

        /// <summary>
        /// Downloads an attachment blob from Azure Blob Storage and returns a stream with its content type and original file name.
        /// </summary>
        /// <param name="noteId">The note's ID (container name).</param>
        /// <param name="attachmentId">The blob name to download.</param>
        /// <returns>
        /// A tuple containing the blob's content stream, content type, and original file name if the blob exists;
        /// <c>null</c> if the blob or container does not exist.
        /// </returns>
        Task<(Stream stream, string contentType, string originalFileName)?> DownloadAttachmentAsync(string noteId, string attachmentId);

        /// <summary>
        /// Lists all attachments in the container associated with the given note.
        /// </summary>
        /// <param name="noteId">The note's ID (container name).</param>
        /// <returns>
        /// A list of attachment info tuples (blobName, originalFileName, contentType, length) if the container exists;
        /// an empty list if the container does not exist or has no blobs.
        /// </returns>
        Task<List<AttachmentInfo>> ListAttachmentsAsync(string noteId);

        /// <summary>
        /// Returns the number of blobs in the container associated with the given note.
        /// Returns 0 if the container does not exist.
        /// </summary>
        /// <param name="noteId">The note's ID (container name).</param>
        /// <returns>The number of blobs in the container.</returns>
        Task<int> GetBlobCountAsync(string noteId);

        /// <summary>
        /// Returns whether a container with the given noteId exists in Azure Blob Storage.
        /// </summary>
        /// <param name="noteId">The note's ID (container name).</param>
        /// <returns><c>true</c> if the container exists; otherwise <c>false</c>.</returns>
        Task<bool> ContainerExistsAsync(string noteId);
    }

    /// <summary>
    /// Represents information about a single attachment blob.
    /// </summary>
    public class AttachmentInfo
    {
        /// <summary>Gets or sets the blob name (GUID-based attachment ID).</summary>
        public string BlobName { get; set; } = string.Empty;

        /// <summary>Gets or sets the original file name stored in blob metadata.</summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Gets or sets the MIME content type of the blob.</summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>Gets or sets the size in bytes of the blob.</summary>
        public long Length { get; set; }
    }
}
