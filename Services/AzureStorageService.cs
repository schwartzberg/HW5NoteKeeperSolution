using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace HW5NoteKeeper.Services
{
    /// <summary>
    /// Provides Azure Blob Storage operations for note attachments.
    /// Attachment containers are named with the note's ID (lowercase GUID).
    /// Each blob stores <c>noteid</c> and <c>originalfilename</c> as metadata.
    /// </summary>
    public class AzureStorageService : IAzureStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<AzureStorageService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="AzureStorageService"/>.
        /// </summary>
        /// <param name="blobServiceClient">The Azure Blob service client used to manage containers and blobs.</param>
        /// <param name="logger">Logger for informational and error messages.</param>
        public AzureStorageService(
            BlobServiceClient blobServiceClient,
            ILogger<AzureStorageService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> UploadAttachmentAsync(string noteId, string attachmentId, IFormFile fileData)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(noteId.ToLowerInvariant());
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            BlobClient blobClient = containerClient.GetBlobClient(attachmentId);
            bool blobAlreadyExists = (await blobClient.ExistsAsync()).Value;

            using Stream stream = fileData.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = fileData.ContentType },
                Metadata = new Dictionary<string, string>
                {
                    { "noteid", noteId },
                    { "originalfilename", fileData.FileName }
                }
            });

            _logger.LogInformation(
                "Blob {AttachmentId} {Action} in container {NoteId} (original: {OriginalFileName})",
                attachmentId,
                blobAlreadyExists ? "updated" : "created",
                noteId,
                fileData.FileName);

            return !blobAlreadyExists;
        }

        /// <inheritdoc/>
        public async Task<AttachmentDeleteResult> DeleteAttachmentAsync(string noteId, string attachmentId)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(noteId.ToLowerInvariant());
            BlobClient blobClient = containerClient.GetBlobClient(attachmentId);

            bool exists = (await blobClient.ExistsAsync()).Value;
            if (!exists)
            {
                return AttachmentDeleteResult.NotFound;
            }

            try
            {
                await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots);
                _logger.LogInformation("Deleted blob {AttachmentId} from container {NoteId}", attachmentId, noteId);
                return AttachmentDeleteResult.Deleted;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to delete blob {AttachmentId} in container {NoteId}", attachmentId, noteId);
                return AttachmentDeleteResult.Error;
            }
        }

        /// <inheritdoc/>
        public async Task<(Stream stream, string contentType, string originalFileName)?> DownloadAttachmentAsync(string noteId, string attachmentId)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(noteId.ToLowerInvariant());

            if (!(await containerClient.ExistsAsync()).Value)
            {
                return null;
            }

            BlobClient blobClient = containerClient.GetBlobClient(attachmentId);

            if (!(await blobClient.ExistsAsync()).Value)
            {
                return null;
            }

            try
            {
                BlobDownloadResult download = await blobClient.DownloadContentAsync();
                Stream stream = download.Content.ToStream();
                string contentType = download.Details.ContentType ?? "application/octet-stream";

                // Retrieve original file name from metadata
                BlobProperties properties = await blobClient.GetPropertiesAsync();
                string originalFileName = properties.Metadata.TryGetValue("originalfilename", out string? name)
                    ? name
                    : attachmentId;

                return (stream, contentType, originalFileName);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to download blob {AttachmentId} from container {NoteId}", attachmentId, noteId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<List<AttachmentInfo>> ListAttachmentsAsync(string noteId)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(noteId.ToLowerInvariant());

            if (!(await containerClient.ExistsAsync()).Value)
            {
                return new List<AttachmentInfo>();
            }

            var attachments = new List<AttachmentInfo>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                BlobProperties properties = await blobClient.GetPropertiesAsync();

                string originalFileName = properties.Metadata.TryGetValue("originalfilename", out string? name)
                    ? name
                    : blobItem.Name;

                attachments.Add(new AttachmentInfo
                {
                    BlobName = blobItem.Name,
                    OriginalFileName = originalFileName,
                    ContentType = blobItem.Properties.ContentType ?? "application/octet-stream",
                    Length = blobItem.Properties.ContentLength ?? 0
                });
            }

            return attachments;
        }

        /// <inheritdoc/>
        public async Task<int> GetBlobCountAsync(string noteId)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(noteId.ToLowerInvariant());

            if (!(await containerClient.ExistsAsync()).Value)
                return 0;

            int count = 0;
            await foreach (BlobItem _ in containerClient.GetBlobsAsync())
                count++;

            return count;
        }

        /// <inheritdoc/>
        public async Task<bool> ContainerExistsAsync(string noteId)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(noteId.ToLowerInvariant());
            return (await containerClient.ExistsAsync()).Value;
        }
    }
}
