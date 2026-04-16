using FluentAssertions;
using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Integration tests for the Notes CRUD Razor Pages.
    /// Uses <see cref="NoteKeeperWebApplicationFactory"/> (InMemory DB + TestAuthHandler)
    /// to drive the full HTTP pipeline — complementing the mock-based unit tests in
    /// <see cref="NotesPageModelTests"/>.
    /// </summary>
    public class NotesIntegrationTests
        : IClassFixture<NoteKeeperWebApplicationFactory>, IAsyncLifetime
    {
        private readonly NoteKeeperWebApplicationFactory _factory;

        private const string UserId = "integration-user-oid";
        private const string OtherUserId = "other-user-oid";

        public NotesIntegrationTests(NoteKeeperWebApplicationFactory factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync() => await _factory.ClearDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        // ── Helpers ──────────────────────────────────────────────────────────────

        private HttpClient CreateClient(string userId = UserId)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
            return client;
        }

        private static async Task<(HttpResponseMessage Response, string? CsrfToken)>
            GetWithCsrfAsync(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();

            // Match <input name="__RequestVerificationToken" ... value="..." />
            var match = Regex.Match(
                html,
                @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
                RegexOptions.Singleline);

            if (!match.Success)
            {
                // Fallback: reversed attribute order
                match = Regex.Match(
                    html,
                    @"<input[^>]+value=""([^""]+)""[^>]+name=""__RequestVerificationToken""",
                    RegexOptions.Singleline);
            }

            return (response, match.Success ? match.Groups[1].Value : null);
        }

        private async Task<Note?> FindNoteAsync(Guid id)
        {
            using var scope = _factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<NoteKeeperContext>();
            return await ctx.Notes.Include(n => n.Tags).FirstOrDefaultAsync(n => n.Id == id);
        }

        private async Task<List<Note>> GetUserNotesAsync(string userId = UserId)
        {
            using var scope = _factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<NoteKeeperContext>();
            return await ctx.Notes.Include(n => n.Tags)
                .Where(n => n.UserRealmId == userId).ToListAsync();
        }

        // ── Create ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_Post_ValidForm_PersistsNoteAndTagInDb()
        {
            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, "/Notes/Create");

            var form = new Dictionary<string, string>
            {
                ["Note.Summary"] = "Integration Note",
                ["Note.Details"] = "Integration Details",
                ["__RequestVerificationToken"] = token!,
            };

            await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(form));

            var notes = await GetUserNotesAsync();
            notes.Should().HaveCount(1);
            notes[0].Summary.Should().Be("Integration Note");
            notes[0].UserRealmId.Should().Be(UserId);
            notes[0].Tags.Should().ContainSingle(t => t.Name == "generated-tag");
        }

        [Fact]
        public async Task Create_Post_ValidForm_RedirectsToIndex()
        {
            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, "/Notes/Create");

            var form = new Dictionary<string, string>
            {
                ["Note.Summary"] = "Redirect Test",
                ["Note.Details"] = "Some details",
                ["__RequestVerificationToken"] = token!,
            };

            var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(form));

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location!.ToString().Should().Contain("/Notes");
        }

        [Fact]
        public async Task Create_Post_EmptySummary_Returns200WithValidationError()
        {
            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, "/Notes/Create");

            var form = new Dictionary<string, string>
            {
                ["Note.Summary"] = "",
                ["Note.Details"] = "Some details",
                ["__RequestVerificationToken"] = token!,
            };

            var response = await client.PostAsync("/Notes/Create", new FormUrlEncodedContent(form));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Summary");
        }

        // ── Edit ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Edit_Post_ChangedDetails_OldTagRemovedNewTagAdded()
        {
            const string original = "Original details";
            var note = await _factory.SeedNoteAsync(UserId, "My Note", original, withTag: true);

            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Edit?id={note.Id}");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["Note.UserRealmId"] = UserId,
                ["Note.Summary"] = "My Note",
                ["Note.Details"] = "Completely new details — changed",
                ["__RequestVerificationToken"] = token!,
            };

            await client.PostAsync("/Notes/Edit", new FormUrlEncodedContent(form));

            var updated = await FindNoteAsync(note.Id);
            updated!.Tags.Should().ContainSingle(t => t.Name == "generated-tag");
            updated.Tags.Should().NotContain(t => t.Name == "seed-tag");
        }

        [Fact]
        public async Task Edit_Post_ChangedDetails_TagsAreNotEmpty()
        {
            var note = await _factory.SeedNoteAsync(UserId, "Tag Check", "Original text", withTag: true);

            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Edit?id={note.Id}");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["Note.UserRealmId"] = UserId,
                ["Note.Summary"] = "Tag Check",
                ["Note.Details"] = "Totally different content to force tag regeneration",
                ["__RequestVerificationToken"] = token!,
            };

            await client.PostAsync("/Notes/Edit", new FormUrlEncodedContent(form));

            var updated = await FindNoteAsync(note.Id);
            updated!.Tags.Should().NotBeEmpty(
                because: "editing a note's details must regenerate tags — they should never be empty");
        }

        [Fact]
        public async Task Edit_Post_ChangedDetails_WithMultipleTags_ReturnsRedirect()
        {
            // Seed a note with multiple tags to exercise orphan deletion
            Note note;
            using (var scope = _factory.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<NoteKeeperContext>();
                note = new Note
                {
                    Id = Guid.NewGuid(),
                    Summary = "Multi-tag note",
                    Details = "Original details",
                    CreatedDateUtc = DateTimeOffset.UtcNow,
                    UserRealmId = UserId,
                };
                ctx.Notes.Add(note);
                ctx.Tags.AddRange(
                    new Tag { NoteId = note.Id, Name = "tag-a" },
                    new Tag { NoteId = note.Id, Name = "tag-b" },
                    new Tag { NoteId = note.Id, Name = "tag-c" });
                await ctx.SaveChangesAsync();
            }

            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Edit?id={note.Id}");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["Note.UserRealmId"] = UserId,
                ["Note.Summary"] = "Multi-tag note",
                ["Note.Details"] = "Completely different content triggers tag regeneration",
                ["__RequestVerificationToken"] = token!,
            };

            var response = await client.PostAsync("/Notes/Edit", new FormUrlEncodedContent(form));

            response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                because: "editing a note with multiple tags must not cause a concurrency exception");

            var updated = await FindNoteAsync(note.Id);
            updated!.Tags.Should().NotContain(t => t.Name == "tag-a",
                because: "old tags should be replaced by newly generated tags");
        }

        [Fact]
        public async Task Edit_Post_ChangedDetails_SplitSave_NoteFieldsAndTagsBothPersisted()
        {
            // Verifies that the two-phase save (DELETE old tags, then UPDATE note + INSERT new tags)
            // persists both note fields and new tags correctly.
            var note = await _factory.SeedNoteAsync(UserId, "Phase Test", "Old details text", withTag: true);

            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Edit?id={note.Id}");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["Note.UserRealmId"] = UserId,
                ["Note.Summary"] = "Updated Phase Summary",
                ["Note.Details"] = "Brand new details for split-save test",
                ["__RequestVerificationToken"] = token!,
            };

            var response = await client.PostAsync("/Notes/Edit", new FormUrlEncodedContent(form));

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            var updated = await FindNoteAsync(note.Id);
            updated.Should().NotBeNull();
            updated!.Summary.Should().Be("Updated Phase Summary",
                because: "note field updates must be saved in the second SaveChangesAsync call");
            updated.Details.Should().Be("Brand new details for split-save test");
            updated.ModifiedDateUtc.Should().NotBeNull();
            updated.Tags.Should().NotBeEmpty(
                because: "new tags must be generated and saved after old tags were deleted");
            updated.Tags.Should().NotContain(t => t.Name == "seed-tag",
                because: "old tags should have been deleted in the first SaveChangesAsync call");
        }

        [Fact]
        public async Task Edit_Post_SameDetails_OriginalTagPreserved()
        {
            const string details = "Unchanged details";
            var note = await _factory.SeedNoteAsync(UserId, "My Note", details, withTag: true);

            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Edit?id={note.Id}");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["Note.UserRealmId"] = UserId,
                ["Note.Summary"] = "Updated Summary",
                ["Note.Details"] = details,          // <-- same, no regeneration
                ["__RequestVerificationToken"] = token!,
            };

            await client.PostAsync("/Notes/Edit", new FormUrlEncodedContent(form));

            var updated = await FindNoteAsync(note.Id);
            updated!.Summary.Should().Be("Updated Summary");
            updated.Tags.Should().ContainSingle(t => t.Name == "seed-tag");
        }

        [Fact]
        public async Task Edit_Post_TamperedUserRealmId_Returns200AndNoteIsUnchanged()
        {
            var note = await _factory.SeedNoteAsync(UserId, "My Note", "My details");

            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Edit?id={note.Id}");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["Note.UserRealmId"] = OtherUserId,    // tampered
                ["Note.Summary"] = "Hacked",
                ["Note.Details"] = "Hacked",
                ["__RequestVerificationToken"] = token!,
            };

            var response = await client.PostAsync("/Notes/Edit", new FormUrlEncodedContent(form));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var unchanged = await FindNoteAsync(note.Id);
            unchanged!.Summary.Should().Be("My Note");
        }

        [Fact]
        public async Task Edit_Get_OtherUserNote_Returns404()
        {
            var note = await _factory.SeedNoteAsync(OtherUserId);

            using var client = CreateClient();
            var response = await client.GetAsync($"/Notes/Edit?id={note.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ── Delete ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_Post_RemovesNoteAndTagsFromDb()
        {
            var note = await _factory.SeedNoteAsync(UserId, withTag: true);

            using var client = CreateClient();
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Delete?id={note.Id}");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["__RequestVerificationToken"] = token!,
            };

            await client.PostAsync($"/Notes/Delete?id={note.Id}", new FormUrlEncodedContent(form));

            var deleted = await FindNoteAsync(note.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task Delete_Post_OtherUserNote_NoteRemainsInDb()
        {
            var note = await _factory.SeedNoteAsync(OtherUserId);

            using var client = CreateClient(UserId);
            // Get a valid CSRF token from Create page (Delete GET returns 404 for other user's note).
            var (_, token) = await GetWithCsrfAsync(client, "/Notes/Create");

            var form = new Dictionary<string, string>
            {
                ["Note.Id"] = note.Id.ToString(),
                ["__RequestVerificationToken"] = token!,
            };

            // Crafted POST — the note doesn't belong to UserId; handler silently skips it.
            var response = await client.PostAsync($"/Notes/Delete?id={note.Id}", new FormUrlEncodedContent(form));

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            var intact = await FindNoteAsync(note.Id);
            intact.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_Get_OtherUserNote_Returns404()
        {
            var note = await _factory.SeedNoteAsync(OtherUserId);

            using var client = CreateClient();
            var response = await client.GetAsync($"/Notes/Delete?id={note.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ── Details rendering (requirement 2.3) ──────────────────────────────────

        /// <summary>
        /// Requirement 2.3: Tags assigned to a note must appear on the Details page.
        /// </summary>
        [Fact]
        public async Task Details_Get_WithTags_ShowsTagNamesInHtml()
        {
            var note = await _factory.SeedNoteAsync(UserId, "Tagged Note", "Note details", withTag: true);

            using var client = CreateClient();
            var response = await client.GetAsync($"/Notes/Details?id={note.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("seed-tag",
                because: "the tag seeded on the note must be visible on the Details page (requirement 2.3)");
        }

        /// <summary>
        /// Requirement 2.3: Tags on the Details page are read-only — there must be no
        /// editable input or textarea element for tags.
        /// </summary>
        [Fact]
        public async Task Details_Get_TagsSection_IsReadOnly_NoTagEditInputs()
        {
            var note = await _factory.SeedNoteAsync(UserId, "Tagged Note", "Note details", withTag: true);

            using var client = CreateClient();
            var response = await client.GetAsync($"/Notes/Details?id={note.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();

            // Tags are displayed as read-only <span class="badge"> elements — no <input> or <textarea>.
            html.Should().NotMatchRegex(@"<input[^>]+name=""[Tt]ag",
                because: "tags are AI-generated and must not be editable (requirement 2.3)");
            html.Should().NotMatchRegex(@"<textarea[^>]+name=""[Tt]ag",
                because: "tags are AI-generated and must not be editable (requirement 2.3)");
        }

        // ── Multi-tenant isolation ────────────────────────────────────────────────

        [Fact]
        public async Task Index_Get_ShowsOnlyCurrentUserNotes()
        {
            await _factory.SeedNoteAsync(UserId, "My Note");
            await _factory.SeedNoteAsync(OtherUserId, "Other User Note");

            using var client = CreateClient();
            var response = await client.GetAsync("/Notes");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("My Note");
            html.Should().NotContain("Other User Note");
        }

        [Fact]
        public async Task Details_Get_OtherUserNote_Returns404()
        {
            var note = await _factory.SeedNoteAsync(OtherUserId);

            using var client = CreateClient();
            var response = await client.GetAsync($"/Notes/Details?id={note.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
