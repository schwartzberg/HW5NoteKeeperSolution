using Azure;
using HW5NoteKeeperSolution.Models;
using HW5NoteKeeperSolution.Settings;
using Microsoft.Extensions.AI;
using NJsonSchema;
using System.Text.Json;

namespace HW5NoteKeeperSolution.Services
{
    /// <summary>
    /// Generates keyword tags from note detail text using Azure OpenAI.
    /// Implements retry with exponential backoff, rate-limit (429) handling,
    /// and structured JSON-schema response format — matching the HW4
    /// <c>TagGeneratorService.GenerateTags</c> pattern.
    /// </summary>
    public class NoteTagService : INoteTagService
    {
        private readonly IChatClient _chatClient;
        private readonly AISettings _aiSettings;
        private readonly ILogger<NoteTagService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="NoteTagService"/>.
        /// </summary>
        /// <param name="chatClient">The AI chat client for communicating with Azure OpenAI.</param>
        /// <param name="aiSettings">Configuration settings for AI model behavior.</param>
        /// <param name="logger">Logger for tracking service operations and errors.</param>
        public NoteTagService(IChatClient chatClient, AISettings aiSettings, ILogger<NoteTagService> logger)
        {
            _chatClient = chatClient;
            _aiSettings = aiSettings;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<KeyTagsResponse> ApplyGeneratedTags(string details, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            int retryDelay = 1000; // Start with 1 second

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    JsonSchema schema = JsonSchema.FromType<KeyTagsResponse>();
                    JsonElement schemaElement = JsonDocument.Parse(schema.ToJson()).RootElement;

                    ChatResponseFormatJson chatResponseFormatJson =
                        ChatResponseFormat.ForJsonSchema(schemaElement, "ChatResponse", "Chat response schema");

                    ChatOptions chatOptions = new ChatOptions()
                    {
                        Temperature = _aiSettings.Temperature,
                        TopP = _aiSettings.TopP,
                        MaxOutputTokens = _aiSettings.MaxOutputTokens,
                        ResponseFormat = chatResponseFormatJson
                    };

                    ChatMessage[] prompt =
                     [
                         new(ChatRole.System,
                               "Return a JSON object with a 'Tags' array containing 3 to 5 one-word keywords that summarize the user's note. " +
                               "Tags must be concise, relevant, and lowercase when possible."),
                              new(ChatRole.User, details)
                     ];

                    ChatResponse? chatResponse = await _chatClient.GetResponseAsync(prompt, chatOptions, cancellationToken);

                    _logger.LogInformation("Attempt {Attempt}: Raw AI response: {Response}, Finish Reason: {FinishReason}",
                        attempt + 1, chatResponse.Text ?? "(null)", chatResponse.FinishReason);

                    if (string.IsNullOrWhiteSpace(chatResponse.Text))
                    {
                        if (attempt < maxRetries - 1)
                        {
                            _logger.LogWarning("AI returned empty response on attempt {Attempt}. Retrying after {Delay}ms...",
                                attempt + 1, retryDelay);
                            await Task.Delay(retryDelay, cancellationToken);
                            retryDelay *= 2; // Exponential backoff
                            continue;
                        }

                        _logger.LogError("AI model returned null or empty response after {Attempts} attempts. Finish Reason: {FinishReason}",
                            maxRetries, chatResponse.FinishReason);
                        throw new InvalidOperationException($"AI model returned null or empty response after {maxRetries} attempts. Finish Reason: {chatResponse.FinishReason}");
                    }

                    var response = JsonSerializer.Deserialize<KeyTagsResponse>(chatResponse.Text);

                    // Validate that the response was successfully deserialized and try again (to max 3 times) if not
                    if (response == null || response.Tags == null || response.Tags.Count == 0)
                    {
                        if (attempt < maxRetries - 1)
                        {
                            _logger.LogWarning("Failed to deserialize or empty tags on attempt {Attempt}. Retrying...", attempt + 1);
                            await Task.Delay(retryDelay, cancellationToken);
                            retryDelay *= 2;
                            continue;
                        }

                        _logger.LogError("Failed to deserialize response after {Attempts} attempts", maxRetries);
                        throw new InvalidOperationException("Failed to deserialize response from AI model");
                    }

                    _logger.LogInformation("Successfully generated {Count} tags on attempt {Attempt}",
                        response.Tags.Count, attempt + 1);

                    return response;
                }
                catch (RequestFailedException ex) when (ex.Status == 429) // Rate limiting
                {
                    if (attempt < maxRetries - 1)
                    {
                        _logger.LogWarning("Rate limited on attempt {Attempt}. Retrying after {Delay}ms...",
                            attempt + 1, retryDelay);
                        await Task.Delay(retryDelay, cancellationToken);
                        retryDelay *= 2;
                        continue;
                    }
                    throw;
                }
            }

            throw new InvalidOperationException("Failed to generate tags after all retry attempts");
        }
    }
}
