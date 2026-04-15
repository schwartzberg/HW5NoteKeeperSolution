namespace HW5NoteKeeperSolution.Settings
{
    public class AISettings
    {
        public string DeploymentUri { get; set; } = string.Empty;

        public string? ApiKey { get; set; }

        public string DeploymentModelName { get; set; } = "gpt-5-mini";

        public float Temperature { get; set; } = 1.0f;

        public float TopP { get; set; } = 1.0f;

        public int MaxOutputTokens { get; set; } = 500;
    }
}
