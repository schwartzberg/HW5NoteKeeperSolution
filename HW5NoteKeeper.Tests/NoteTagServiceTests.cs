using Azure;
using FluentAssertions;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;
using HW5NoteKeeper.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Unit tests for <see cref="NoteTagService"/> using a mocked <see cref="IChatClient"/>.
    /// Tests retry behavior, rate-limit (429) handling, and correct AI prompt construction.
    /// </summary>
    public class NoteTagServiceTests
    {
        private readonly Mock<IChatClient> _chatClientMock;
        private readonly AISettings _aiSettings;

        public NoteTagServiceTests()
        {
            _chatClientMock = new Mock<IChatClient>();
            _aiSettings = new AISettings
            {
                Temperature = 1.0f,
                TopP = 1.0f,
                MaxOutputTokens = 500,
                DeploymentModelName = "test-model"
            };
        }

        private NoteTagService CreateService() =>
            new NoteTagService(_chatClientMock.Object, _aiSettings, NullLogger<NoteTagService>.Instance);

        private void SetupChatResponse(string jsonText)
        {
            _chatClientMock
                .Setup(c => c.GetResponseAsync(
                    It.IsAny<IList<ChatMessage>>(),
                    It.IsAny<ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, jsonText)));
        }

        private static string TagsJson(params string[] tags) =>
            JsonSerializer.Serialize(new KeyTagsResponse { Tags = tags.ToList() });

        [Fact]
        public async Task ApplyGeneratedTags_ValidDetails_ReturnsTagsResponse()
        {
            SetupChatResponse(TagsJson("cloud", "azure", "computing"));
            NoteTagService service = CreateService();

            KeyTagsResponse result = await service.ApplyGeneratedTags("Some note about cloud computing on Azure");

            result.Tags.Should().HaveCount(3);
            result.Tags.Should().Contain("cloud");
            result.Tags.Should().Contain("azure");
            result.Tags.Should().Contain("computing");
        }

        [Fact]
        public async Task ApplyGeneratedTags_EmptyResponseAllAttempts_ThrowsInvalidOperationException()
        {
            SetupChatResponse("");
            NoteTagService service = CreateService();

            await service.Invoking(s => s.ApplyGeneratedTags("test details"))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*empty response*");
        }

        [Fact]
        public async Task ApplyGeneratedTags_NullTagsInResponse_ThrowsInvalidOperationException()
        {
            SetupChatResponse("{\"Tags\":null}");
            NoteTagService service = CreateService();

            await service.Invoking(s => s.ApplyGeneratedTags("test details"))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*deserialize*");
        }

        [Fact]
        public async Task ApplyGeneratedTags_EmptyTagsArray_ThrowsInvalidOperationException()
        {
            SetupChatResponse("{\"Tags\":[]}");
            NoteTagService service = CreateService();

            await service.Invoking(s => s.ApplyGeneratedTags("test details"))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*deserialize*");
        }

        [Fact]
        public async Task ApplyGeneratedTags_RateLimited429_RetriesAndThrows()
        {
            _chatClientMock
                .Setup(c => c.GetResponseAsync(
                    It.IsAny<IList<ChatMessage>>(),
                    It.IsAny<ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(429, "Rate limited"));

            NoteTagService service = CreateService();

            await service.Invoking(s => s.ApplyGeneratedTags("test details"))
                .Should().ThrowAsync<RequestFailedException>();
        }

        [Fact]
        public async Task ApplyGeneratedTags_SucceedsOnSecondAttempt_ReturnsResponse()
        {
            int callCount = 0;
            _chatClientMock
                .Setup(c => c.GetResponseAsync(
                    It.IsAny<IList<ChatMessage>>(),
                    It.IsAny<ChatOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    string text = callCount == 1 ? "" : TagsJson("cloud");
                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
                });

            NoteTagService service = CreateService();

            KeyTagsResponse result = await service.ApplyGeneratedTags("test details");

            result.Tags.Should().Contain("cloud");
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task ApplyGeneratedTags_CallsChatClientWithCorrectRoles()
        {
            SetupChatResponse(TagsJson("test"));
            NoteTagService service = CreateService();

            await service.ApplyGeneratedTags("user text");

            _chatClientMock.Verify(c => c.GetResponseAsync(
                It.Is<IList<ChatMessage>>(msgs =>
                    msgs.Count == 2 &&
                    msgs[0].Role == ChatRole.System &&
                    msgs[1].Role == ChatRole.User &&
                    msgs[1].Text == "user text"),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
