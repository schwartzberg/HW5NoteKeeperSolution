namespace HW5NoteKeeperSolution.Settings
{
    public class StorageOperationalSettings
    {
        public string ZipRequestsQueueName { get; set; } = "attachment-zip-requests";

        public string ZipPoisonQueueName { get; set; } = "attachment-zip-requests-poison";

        public List<string> ProtectedContainers { get; set; } = new()
        {
            "app-package-func-hw4",
            "azure-webjobs-hosts",
            "azure-webjobs-secrets"
        };
    }
}
