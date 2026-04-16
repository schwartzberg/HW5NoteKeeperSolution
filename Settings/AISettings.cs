namespace HW5NoteKeeperSolution.Settings
{
    /// <summary>
    /// Configuration settings for the Azure OpenAI deployment used to generate note tags.
    /// Bound from the <c>AzureOpenAI</c> configuration section.
    /// </summary>
    public class AISettings
    {
        /// <summary>Gets or sets the full URI of the Azure OpenAI deployment endpoint.</summary>
        public string DeploymentUri { get; set; } = "https://ai-csscie94-foundry.openai.azure.com/";

        /// <summary>Gets or sets an optional API key. When <see langword="null"/> or empty, <c>DefaultAzureCredential</c> is used instead.</summary>
        public string? ApiKey { get; set; }

        /// <summary>Gets or sets the model deployment name. Defaults to <c>gpt-5-mini</c>.</summary>
        public string DeploymentModelName { get; set; } = "gpt-5-mini";

        /// <summary>Gets or sets the sampling temperature for generation. Defaults to <c>1.0</c>.</summary>
        public float Temperature { get; set; } = 1.0f;

        /// <summary>Gets or sets the nucleus-sampling probability mass. Defaults to <c>1.0</c>.</summary>
        public float TopP { get; set; } = 1.0f;

        /// <summary>Gets or sets the maximum number of tokens in the generated response. Defaults to <c>500</c>.</summary>
        public int MaxOutputTokens { get; set; } = 500;
    }
}
