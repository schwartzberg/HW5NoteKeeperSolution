namespace HW5NoteKeeperSolution.Settings
{
    public class StorageAccountSettings
    {
        public string? ContainerEndpoint { get; set; }

        public string? TableEndpoint { get; set; }

        public string? QueueEndpoint { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        public string? AccountKey { get; set; }

        public string Url { get; set; } = string.Empty;
    }
}
