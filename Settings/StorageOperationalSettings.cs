namespace HW5NoteKeeperSolution.Settings
{
    /// <summary>
    /// Operational settings for Azure Storage queue and container names used by the application.
    /// Bound from the <c>StorageOperationalSettings</c> configuration section.
    /// </summary>
    public class StorageOperationalSettings
    {
        /// <summary>Gets or sets the name of the queue that receives attachment-zip work items. Defaults to <c>attachment-zip-requests</c>.</summary>
        public string ZipRequestsQueueName { get; set; } = "attachment-zip-requests";

        /// <summary>Gets or sets the name of the dead-letter (poison) queue for failed zip requests. Defaults to <c>attachment-zip-requests-poison</c>.</summary>
        public string ZipPoisonQueueName { get; set; } = "attachment-zip-requests-poison";

        /// <summary>
        /// Gets or sets the list of container names that must never be deleted or overwritten by application code.
        /// Defaults to the standard Azure Functions infrastructure containers.
        /// </summary>
        public List<string> ProtectedContainers { get; set; } = new()
        {
            "app-package-func-hw4",
            "azure-webjobs-hosts",
            "azure-webjobs-secrets"
        };
    }
}
