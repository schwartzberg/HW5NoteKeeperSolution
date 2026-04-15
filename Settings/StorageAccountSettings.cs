namespace HW5NoteKeeperSolution.Settings
{
    /// <summary>
    /// Connection and identity settings for the Azure Storage account used by the application.
    /// Bound from the <c>StorageAccountSettings</c> configuration section.
    /// </summary>
    public class StorageAccountSettings
    {
        /// <summary>Gets or sets an explicit blob container endpoint URI. When <see langword="null"/> the endpoint is derived from <see cref="Url"/>.</summary>
        public string? ContainerEndpoint { get; set; }

        /// <summary>Gets or sets an explicit Azure Table Storage endpoint URI. When <see langword="null"/> the endpoint is derived from <see cref="Url"/>.</summary>
        public string? TableEndpoint { get; set; }

        /// <summary>Gets or sets an explicit Azure Queue Storage endpoint URI. When <see langword="null"/> the endpoint is derived from <see cref="Url"/>.</summary>
        public string? QueueEndpoint { get; set; }

        /// <summary>Gets or sets the Entra tenant ID used to scope <c>DefaultAzureCredential</c>.</summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>Gets or sets the storage account name.</summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>Gets or sets an optional storage account key. When <see langword="null"/> or empty, managed identity is used.</summary>
        public string? AccountKey { get; set; }

        /// <summary>Gets or sets the primary blob service endpoint URL (e.g., <c>https://&lt;account&gt;.blob.core.windows.net</c>).</summary>
        public string Url { get; set; } = string.Empty;
    }
}
