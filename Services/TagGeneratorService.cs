using HW5NoteKeeperSolution.Models;
using HW5NoteKeeperSolution.Settings;
using Microsoft.Extensions.AI;
using NJsonSchema;
using System.Text.Json;

namespace HW5NoteKeeperSolution.Services
{
    /// <summary>
    /// Calls the Azure OpenAI chat completion endpoint to generate keyword tags from a note's details.
    /// Sends a structured JSON-schema response format request and deserializes the result into
    /// a <see cref="KeyTagsResponse"/>.
    /// </summary>
    public class TagGeneratorService : ITagGeneratorService
    {
        private readonly IChatClient _chatClient;
        private readonly AISettings _settings;
        private readonly ILogger<TagGeneratorService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="TagGeneratorService"/>.
        /// </summary>
        /// <param name="chatClient">The <see cref="IChatClient"/> used to call the Azure OpenAI endpoint.</param>
        /// <param name="settings">AI model settings (temperature, top-p, max tokens, etc.).</param>
        /// <param name="logger">Logger for generation diagnostics.</param>
        public TagGeneratorService(
            IChatClient chatClient,
            AISettings settings,
            ILogger<TagGeneratorService> logger)
        {
            _chatClient = chatClient;
            _settings = settings;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<KeyTagsResponse> GenerateTagsAsync(string details, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(details))
            {
                throw new InvalidOperationException("Note details are required to generate tags.");
            }

            JsonSchema schema = JsonSchema.FromType<KeyTagsResponse>();
            JsonElement schemaElement = JsonDocument.Parse(schema.ToJson()).RootElement;
            ChatResponseFormatJson responseFormat = ChatResponseFormat.ForJsonSchema(
                schemaElement,
                "KeyTagsResponse",
                "Generated tags schema");

            ChatOptions options = new()
            {
                Temperature = _settings.Temperature,
                TopP = _settings.TopP,
                MaxOutputTokens = _settings.MaxOutputTokens,
                ResponseFormat = responseFormat
            };

            ChatMessage[] prompt =
            [
                new(ChatRole.System,
                    "Return a JSON object with a 'Tags' array containing 3 to 5 one-word keywords that summarize the user's note. " +
                    "Tags must be concise, relevant, and lowercase when possible."),
                new(ChatRole.User, details)
            ];

            ChatResponse response = await _chatClient.GetResponseAsync(prompt, options, cancellationToken);
            _logger.LogInformation("Generated tag response with finish reason {FinishReason}.", response.FinishReason);

            if (string.IsNullOrWhiteSpace(response.Text))
            {
                throw new InvalidOperationException("Azure OpenAI returned an empty tag response.");
            }

            KeyTagsResponse? parsedResponse = JsonSerializer.Deserialize<KeyTagsResponse>(response.Text);
            if (parsedResponse?.Tags == null || parsedResponse.Tags.Count == 0)
            {
                throw new InvalidOperationException("Azure OpenAI returned a tag response that could not be parsed.");
            }

            return parsedResponse;
        }
    }
}
