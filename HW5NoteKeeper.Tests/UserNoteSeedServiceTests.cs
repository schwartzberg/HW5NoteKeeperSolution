using FluentAssertions;
using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Unit tests for <see cref="UserNoteSeedService"/> using an in-memory database.
    /// These tests exercise per-user seeding behavior without touching live Azure services.
    /// </summary>
    public class UserNoteSeedServiceTests : IDisposable
    {
        private readonly NoteKeeperContext _context;
        private readonly Mock<INoteTagService> _noteTagServiceMock;
        private readonly Mock<IAzureStorageInitializer> _storageInitializerMock;
        private readonly IMemoryCache _memoryCache;

        public UserNoteSeedServiceTests()
        {
            DbContextOptions<NoteKeeperContext> options = new DbContextOptionsBuilder<NoteKeeperContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new NoteKeeperContext(options);

            _noteTagServiceMock = new Mock<INoteTagService>();
            _noteTagServiceMock
                .Setup(s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new KeyTagsResponse { Tags = new List<string> { "tag1" } });

            _storageInitializerMock = new Mock<IAzureStorageInitializer>();
            _storageInitializerMock
                .Setup(s => s.InitializeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        public void Dispose()
        {
            _context.Dispose();
            _memoryCache.Dispose();
        }

        private UserNoteSeedService CreateService() =>
            new UserNoteSeedService(
                _context,
                _noteTagServiceMock.Object,
                _storageInitializerMock.Object,
                _memoryCache,
                NullLogger<UserNoteSeedService>.Instance);

        [Fact]
        public async Task EnsureSeedDataAsync_NewUser_CreatesFourNotes()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(userRealmId);

            List<Note> notes = await _context.Notes
                .Where(n => n.UserRealmId == userRealmId)
                .ToListAsync();

            notes.Should().HaveCount(4);
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NewUser_NotesHaveExpectedSummaries()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(userRealmId);

            List<string> summaries = await _context.Notes
                .Where(n => n.UserRealmId == userRealmId)
                .Select(n => n.Summary)
                .ToListAsync();

            summaries.Should().Contain("Running grocery list");
            summaries.Should().Contain("Gift supplies notes");
            summaries.Should().Contain("Valentine's Day gift ideas");
            summaries.Should().Contain("Azure tips");
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NewUser_AllNotesHaveUserRealmId()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(userRealmId);

            bool allHaveRealm = await _context.Notes
                .Where(n => n.UserRealmId == userRealmId)
                .AllAsync(n => n.UserRealmId == userRealmId);

            allHaveRealm.Should().BeTrue();
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NewUser_CallsTagServiceForEachNote()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(userRealmId);

            _noteTagServiceMock.Verify(
                s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(4));
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NewUser_CallsStorageInitializerForEachNote()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(userRealmId);

            _storageInitializerMock.Verify(
                s => s.InitializeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Exactly(4));
        }

        [Fact]
        public async Task EnsureSeedDataAsync_CalledTwice_DoesNotDuplicateNotes()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(userRealmId);
            await service.EnsureSeedDataAsync(userRealmId);

            int count = await _context.Notes.CountAsync(n => n.UserRealmId == userRealmId);
            count.Should().Be(4, "second call should be short-circuited by in-memory cache");
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NotesAlreadyInDb_SkipsSeeding()
        {
            string userRealmId = Guid.NewGuid().ToString();

            // Pre-seed 4 notes directly into the DB to simulate a prior run
            string[] summaries = ["Running grocery list", "Gift supplies notes", "Valentine's Day gift ideas", "Azure tips"];
            foreach (string summary in summaries)
            {
                Guid noteId = Guid.NewGuid();
                Note note = new Note
                {
                    Id = noteId,
                    Summary = summary,
                    Details = "details",
                    CreatedDateUtc = DateTimeOffset.UtcNow,
                    UserRealmId = userRealmId
                };
                note.Tags.Add(new Tag { Id = Guid.NewGuid(), NoteId = noteId, Name = "existing" });
                _context.Notes.Add(note);
            }
            await _context.SaveChangesAsync();

            UserNoteSeedService service = CreateService();
            await service.EnsureSeedDataAsync(userRealmId);

            int count = await _context.Notes.CountAsync(n => n.UserRealmId == userRealmId);
            count.Should().Be(4, "user already has notes so seeding should be skipped entirely");

            _noteTagServiceMock.Verify(
                s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "tag service should not be called when user already has notes");
        }

        [Fact]
        public async Task EnsureSeedDataAsync_UserHasOneCustomNote_SkipsSeeding()
        {
            string userRealmId = Guid.NewGuid().ToString();

            // Pre-seed a single custom note (not a seed note)
            _context.Notes.Add(new Note
            {
                Id = Guid.NewGuid(),
                Summary = "My custom note",
                Details = "Some details",
                CreatedDateUtc = DateTimeOffset.UtcNow,
                UserRealmId = userRealmId
            });
            await _context.SaveChangesAsync();

            UserNoteSeedService service = CreateService();
            await service.EnsureSeedDataAsync(userRealmId);

            int count = await _context.Notes.CountAsync(n => n.UserRealmId == userRealmId);
            count.Should().Be(1, "user already has a note so no seed notes should be added");

            _noteTagServiceMock.Verify(
                s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _storageInitializerMock.Verify(
                s => s.InitializeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EnsureSeedDataAsync_OtherUserHasNotes_StillSeedsNewUser()
        {
            string existingUser = Guid.NewGuid().ToString();
            string newUser = Guid.NewGuid().ToString();

            // Existing user has notes
            _context.Notes.Add(new Note
            {
                Id = Guid.NewGuid(),
                Summary = "Existing user note",
                Details = "details",
                CreatedDateUtc = DateTimeOffset.UtcNow,
                UserRealmId = existingUser
            });
            await _context.SaveChangesAsync();

            UserNoteSeedService service = CreateService();
            await service.EnsureSeedDataAsync(newUser);

            int newUserCount = await _context.Notes.CountAsync(n => n.UserRealmId == newUser);
            newUserCount.Should().Be(4, "new user should be seeded regardless of other users' notes");
        }

        [Fact]
        public async Task EnsureSeedDataAsync_TwoDifferentUsers_SeedsBothIndependently()
        {
            string user1 = Guid.NewGuid().ToString();
            string user2 = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(user1);
            await service.EnsureSeedDataAsync(user2);

            int count1 = await _context.Notes.CountAsync(n => n.UserRealmId == user1);
            int count2 = await _context.Notes.CountAsync(n => n.UserRealmId == user2);

            count1.Should().Be(4);
            count2.Should().Be(4);
        }

        [Fact]
        public async Task EnsureSeedDataAsync_EmptyUserRealmId_Throws()
        {
            UserNoteSeedService service = CreateService();

            await service.Invoking(s => s.EnsureSeedDataAsync(string.Empty))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*object identifier*");
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NullUserRealmId_Throws()
        {
            UserNoteSeedService service = CreateService();

            await service.Invoking(s => s.EnsureSeedDataAsync(null!))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NewUser_EachNoteHasAtLeastOneTag()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            await service.EnsureSeedDataAsync(userRealmId);

            List<Note> notes = await _context.Notes
                .Include(n => n.Tags)
                .Where(n => n.UserRealmId == userRealmId)
                .ToListAsync();

            notes.Should().AllSatisfy(note => note.Tags.Should().NotBeEmpty());
        }

        [Fact]
        public async Task EnsureSeedDataAsync_NewUser_NotesHaveCreatedDateUtc()
        {
            string userRealmId = Guid.NewGuid().ToString();
            UserNoteSeedService service = CreateService();

            DateTimeOffset before = DateTimeOffset.UtcNow;
            await service.EnsureSeedDataAsync(userRealmId);
            DateTimeOffset after = DateTimeOffset.UtcNow;

            List<Note> notes = await _context.Notes
                .Where(n => n.UserRealmId == userRealmId)
                .ToListAsync();

            notes.Should().AllSatisfy(note =>
                note.CreatedDateUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after));
        }
    }
}
