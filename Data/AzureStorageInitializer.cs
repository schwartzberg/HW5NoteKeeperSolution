using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HW5NoteKeeperSolution.Settings;

namespace HW5NoteKeeperSolution.Data
{
    public class AzureStorageInitializer : IAzureStorageInitializer
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly StorageOperationalSettings _operationalSettings;
        private readonly ILogger<AzureStorageInitializer> _logger;

        private static readonly Dictionary<string, string[]> _attachmentMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Running grocery list", new[] { "MilkAndEggs.png", "Oranges.png" } },
            { "Gift supplies notes", new[] { "WrappingPaper.png", "Tape.png" } },
            { "Valentine's Day gift ideas", new[] { "Chocolate.png", "Diamonds.png", "NewCar.png" } },
            { "Azure tips", new[] { "AzureLogo.png", "AzureTipsAndTricks.pdf" } }
        };

        public AzureStorageInitializer(
            BlobServiceClient blobServiceClient,
            StorageOperationalSettings operationalSettings,
            ILogger<AzureStorageInitializer> logger)
        {
            _blobServiceClient = blobServiceClient;
            _operationalSettings = operationalSettings;
            _logger = logger;
        }

        public IReadOnlyDictionary<string, string[]> AttachmentMapping => _attachmentMapping;

        public async Task<bool> InitializeAsync(
            Guid noteId,
            string summary,
            string? attachmentsDirectory = null,
            CancellationToken cancellationToken = default)
        {
            attachmentsDirectory ??= Path.Combine(AppContext.BaseDirectory, "AzureStorageAttachments");

            if (!_attachmentMapping.TryGetValue(summary, out string[]? attachmentFiles))
            {
                _logger.LogInformation("No seed attachments are mapped for note summary '{Summary}'.", summary);
                return true;
            }

            string containerName = noteId.ToString().ToLowerInvariant();
            if (_operationalSettings.ProtectedContainers.Contains(containerName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Seed container name '{containerName}' conflicts with a protected container.");
            }

            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            foreach (string fileName in attachmentFiles)
            {
                string filePath = Path.Combine(attachmentsDirectory, fileName);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Seed attachment '{fileName}' was not found.", filePath);
                }

                BlobClient blobClient = containerClient.GetBlobClient(fileName);
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

                await using FileStream stream = File.OpenRead(filePath);
                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = GetContentType(fileName)
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            ["noteid"] = noteId.ToString()
                        }
                    },
                    cancellationToken);
            }

            return true;
        }

        private static string GetContentType(string fileName) =>
            Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
    }
}
