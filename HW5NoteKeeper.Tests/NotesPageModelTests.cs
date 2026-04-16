using FluentAssertions;
using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Pages.Notes;
using HW5NoteKeeper.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Unit tests for all Notes Razor Page models (Index, Create, Edit, Details, Delete).
    /// Covers multi-tenant security (UserRealmId filtering), tag generation on Create and Edit,
    /// and tag regeneration only when Details change.
    /// </summary>
    public class NotesPageModelTests : IDisposable
    {
        private readonly DbContextOptions<NoteKeeperContext> _options;
        private readonly NoteKeeperContext _context;
        private readonly Mock<INoteTagService> _tagServiceMock;
        private readonly Mock<IUserNoteSeedService> _seedServiceMock;
        private readonly Mock<IAzureStorageService> _storageServiceMock;
        private readonly Settings.NoteLimits _noteLimits;

        private const string UserId = "user-oid-abc";
        private const string OtherUserId = "user-oid-xyz";

        public NotesPageModelTests()
        {
            _options = new DbContextOptionsBuilder<NoteKeeperContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new NoteKeeperContext(_options);

            _tagServiceMock = new Mock<INoteTagService>();
            _seedServiceMock = new Mock<IUserNoteSeedService>();
            _storageServiceMock = new Mock<IAzureStorageService>();
            _noteLimits = new Settings.NoteLimits();

            _tagServiceMock
                .Setup(s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new KeyTagsResponse { Tags = new List<string> { "generated-tag" } });

            _storageServiceMock
                .Setup(s => s.ListAttachmentsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<AttachmentInfo>());
        }

        public void Dispose() => _context.Dispose();

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static ClaimsPrincipal MakePrincipal(string userId)
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(AzureADClaimTypes.ObjectId, userId) }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private static void SetUser(PageModel model, ClaimsPrincipal user)
        {
            var httpContext = new DefaultHttpContext { User = user };
            model.PageContext = new PageContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor()
            };
        }

        private Note SeedNote(string userId, string summary = "Test Note",
            string details = "Test details", bool withTag = false)
        {
            var note = new Note
            {
                Id = Guid.NewGuid(),
                Summary = summary,
                Details = details,
                UserRealmId = userId,
                CreatedDateUtc = DateTimeOffset.UtcNow
            };

            if (withTag)
            {
                note.Tags.Add(new Tag { Id = Guid.NewGuid(), NoteId = note.Id, Name = "seed-tag" });
            }

            // Use a separate context so the test's _context loads entities fresh
            // (avoids EF identity-map issues where pre-tracked entities lack ObservableHashSet)
            using var seedContext = new NoteKeeperContext(_options);
            seedContext.Notes.Add(note);
            seedContext.SaveChanges();
            return note;
        }

        // ── IndexModel ───────────────────────────────────────────────────────────

        [Fact]
        public async Task Index_OnGetAsync_ReturnsOnlyCurrentUserNotes()
        {
            SeedNote(UserId, "My Note");
            SeedNote(OtherUserId, "Other Note");

            var model = new IndexModel(_context, _seedServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));

            await model.OnGetAsync();

            model.Note.Should().HaveCount(1);
            model.Note[0].Summary.Should().Be("My Note");
        }

        [Fact]
        public async Task Index_OnGetAsync_ExcludesOtherUserNotes()
        {
            SeedNote(OtherUserId, "Other Note 1");
            SeedNote(OtherUserId, "Other Note 2");

            var model = new IndexModel(_context, _seedServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));

            await model.OnGetAsync();

            model.Note.Should().BeEmpty();
        }

        // ── CreateModel ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_OnPostAsync_SetsUserRealmIdFromClaims()
        {
            var model = new CreateModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note { Summary = "New Note", Details = "Some details" };

            var result = await model.OnPostAsync();

            var saved = await _context.Notes.FirstAsync();
            saved.UserRealmId.Should().Be(UserId);
        }

        [Fact]
        public async Task Create_OnPostAsync_SetsCreatedDateUtc()
        {
            var before = DateTimeOffset.UtcNow.AddSeconds(-1);
            var model = new CreateModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note { Summary = "New Note", Details = "Some details" };

            await model.OnPostAsync();

            var saved = await _context.Notes.FirstAsync();
            saved.CreatedDateUtc.Should().BeAfter(before);
        }

        [Fact]
        public async Task Create_OnPostAsync_CallsTagServiceAndPersistsTags()
        {
            var model = new CreateModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note { Summary = "New Note", Details = "Some details" };

            await model.OnPostAsync();

            _tagServiceMock.Verify(
                s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);

            var tags = await _context.Tags.ToListAsync();
            tags.Should().HaveCount(1);
            tags[0].Name.Should().Be("generated-tag");
        }

        [Fact]
        public async Task Create_OnPostAsync_RedirectsToIndex()
        {
            var model = new CreateModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note { Summary = "New Note", Details = "Some details" };

            var result = await model.OnPostAsync();

            result.Should().BeOfType<RedirectToPageResult>()
                .Which.PageName.Should().Be("./Index");
        }

        [Fact]
        public async Task Create_OnPostAsync_InvalidModelState_ReturnsPage()
        {
            var model = new CreateModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note { Summary = "New Note", Details = "Some details" };
            model.ModelState.AddModelError("Summary", "Required");

            var result = await model.OnPostAsync();

            result.Should().BeOfType<PageResult>();
        }

        // ── DetailsModel ─────────────────────────────────────────────────────────

        private DetailsModel MakeDetailsModel() =>
            new DetailsModel(_context, _storageServiceMock.Object, _noteLimits,
                new Mock<ILogger<DetailsModel>>().Object);

        [Fact]
        public async Task Details_OnGetAsync_WithOwnNote_ReturnsPageWithNote()
        {
            var note = SeedNote(UserId, withTag: true);

            var model = MakeDetailsModel();
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnGetAsync(note.Id);

            result.Should().BeOfType<PageResult>();
            model.Note.Id.Should().Be(note.Id);
        }

        [Fact]
        public async Task Details_OnGetAsync_IncludesTags()
        {
            var note = SeedNote(UserId, withTag: true);

            var model = MakeDetailsModel();
            SetUser(model, MakePrincipal(UserId));

            await model.OnGetAsync(note.Id);

            model.Note.Tags.Should().HaveCount(1);
            model.Note.Tags.First().Name.Should().Be("seed-tag");
        }

        [Fact]
        public async Task Details_OnGetAsync_OtherUserNote_ReturnsNotFound()
        {
            var note = SeedNote(OtherUserId);

            var model = MakeDetailsModel();
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnGetAsync(note.Id);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Details_OnGetAsync_NullId_ReturnsNotFound()
        {
            var model = MakeDetailsModel();
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnGetAsync(null);

            result.Should().BeOfType<NotFoundResult>();
        }

        // ── EditModel ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Edit_OnGetAsync_WithOwnNote_ReturnsPage()
        {
            var note = SeedNote(UserId);

            var model = new EditModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnGetAsync(note.Id);

            result.Should().BeOfType<PageResult>();
            model.Note.Id.Should().Be(note.Id);
        }

        [Fact]
        public async Task Edit_OnGetAsync_OtherUserNote_ReturnsNotFound()
        {
            var note = SeedNote(OtherUserId);

            var model = new EditModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnGetAsync(note.Id);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Edit_OnPostAsync_WithChangedDetails_RegeneratesTags()
        {
            var note = SeedNote(UserId, details: "Original details", withTag: true);

            var model = new EditModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note
            {
                Id = note.Id,
                UserRealmId = UserId,
                Summary = note.Summary,
                Details = "Updated details — completely different"
            };

            await model.OnPostAsync();

            _tagServiceMock.Verify(
                s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Edit_OnPostAsync_WithSameDetails_DoesNotRegenerateTags()
        {
            const string sameDetails = "Unchanged details";
            var note = SeedNote(UserId, details: sameDetails, withTag: true);

            var model = new EditModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note
            {
                Id = note.Id,
                UserRealmId = UserId,
                Summary = "Updated Summary",
                Details = sameDetails
            };

            await model.OnPostAsync();

            _tagServiceMock.Verify(
                s => s.ApplyGeneratedTags(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Edit_OnPostAsync_WithChangedDetails_SetsModifiedDateUtc()
        {
            var note = SeedNote(UserId, details: "Original");
            var before = DateTimeOffset.UtcNow.AddSeconds(-1);

            var model = new EditModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note
            {
                Id = note.Id,
                UserRealmId = UserId,
                Summary = note.Summary,
                Details = "Changed details"
            };

            await model.OnPostAsync();

            var updated = await _context.Notes.FindAsync(note.Id);
            updated!.ModifiedDateUtc.Should().NotBeNull();
            updated.ModifiedDateUtc!.Value.Should().BeAfter(before);
        }

        [Fact]
        public async Task Edit_OnPostAsync_WrongUserRealmId_AddsModelStateError()
        {
            var note = SeedNote(UserId);

            var model = new EditModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note
            {
                Id = note.Id,
                UserRealmId = OtherUserId,    // tampered — does not match authenticated user
                Summary = note.Summary,
                Details = note.Details
            };

            var result = await model.OnPostAsync();

            result.Should().BeOfType<PageResult>();
            model.ModelState.IsValid.Should().BeFalse();
            model.ModelState.ContainsKey("realm").Should().BeTrue();
        }

        [Fact]
        public async Task Edit_OnPostAsync_ValidEdit_RedirectsToIndex()
        {
            var note = SeedNote(UserId);

            var model = new EditModel(_context, _tagServiceMock.Object);
            SetUser(model, MakePrincipal(UserId));
            model.Note = new Note
            {
                Id = note.Id,
                UserRealmId = UserId,
                Summary = "Updated",
                Details = "Updated details"
            };

            var result = await model.OnPostAsync();

            result.Should().BeOfType<RedirectToPageResult>()
                .Which.PageName.Should().Be("./Index");
        }

        // ── DeleteModel ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_OnGetAsync_WithOwnNote_ReturnsPageWithTags()
        {
            var note = SeedNote(UserId, withTag: true);

            var model = new DeleteModel(_context);
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnGetAsync(note.Id);

            result.Should().BeOfType<PageResult>();
            model.Note.Tags.Should().HaveCount(1);
        }

        [Fact]
        public async Task Delete_OnGetAsync_OtherUserNote_ReturnsNotFound()
        {
            var note = SeedNote(OtherUserId);

            var model = new DeleteModel(_context);
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnGetAsync(note.Id);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_OnPostAsync_WithOwnNote_RemovesNoteFromDatabase()
        {
            var note = SeedNote(UserId, withTag: true);

            var model = new DeleteModel(_context);
            SetUser(model, MakePrincipal(UserId));

            await model.OnPostAsync(note.Id);

            var found = await _context.Notes.FindAsync(note.Id);
            found.Should().BeNull();
        }

        [Fact]
        public async Task Delete_OnPostAsync_WithOwnNote_TagsAreAlsoRemoved()
        {
            var note = SeedNote(UserId, withTag: true);

            var model = new DeleteModel(_context);
            SetUser(model, MakePrincipal(UserId));

            await model.OnPostAsync(note.Id);

            var tags = await _context.Tags.Where(t => t.NoteId == note.Id).ToListAsync();
            tags.Should().BeEmpty();
        }

        [Fact]
        public async Task Delete_OnPostAsync_OtherUserNote_DoesNotDeleteNote()
        {
            var note = SeedNote(OtherUserId);

            var model = new DeleteModel(_context);
            SetUser(model, MakePrincipal(UserId));

            await model.OnPostAsync(note.Id);

            var found = await _context.Notes.FindAsync(note.Id);
            found.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_OnPostAsync_RedirectsToIndex()
        {
            var note = SeedNote(UserId);

            var model = new DeleteModel(_context);
            SetUser(model, MakePrincipal(UserId));

            var result = await model.OnPostAsync(note.Id);

            result.Should().BeOfType<RedirectToPageResult>()
                .Which.PageName.Should().Be("./Index");
        }
    }
}
