using FluentAssertions;
using HW5NoteKeeper.Services;
using HW5NoteKeeper.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Integration tests for attachment management on the Details page.
    /// Uses <see cref="NoteKeeperWebApplicationFactory"/> with an
    /// <see cref="NoteKeeperWebApplicationFactory.InMemoryAzureStorageService"/>
    /// so that upload, list, download, and delete operations can be verified
    /// end-to-end without Azure Blob Storage.
    /// Modeled after the HW4 NoteKeeperAttachmentE2ETests pattern.
    /// </summary>
    public class AttachmentIntegrationTests
        : IClassFixture<NoteKeeperWebApplicationFactory>, IAsyncLifetime
    {
        private readonly NoteKeeperWebApplicationFactory _factory;
        private const string UserId = "attachment-test-oid";
        private const string OtherUserId = "other-user-oid";

        public AttachmentIntegrationTests(NoteKeeperWebApplicationFactory factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            await _factory.ClearDatabaseAsync();
            _factory.ClearStorage();
        }

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

            var match = Regex.Match(
                html,
                @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
                RegexOptions.Singleline);

            if (!match.Success)
            {
                match = Regex.Match(
                    html,
                    @"<input[^>]+value=""([^""]+)""[^>]+name=""__RequestVerificationToken""",
                    RegexOptions.Singleline);
            }

            return (response, match.Success ? match.Groups[1].Value : null);
        }

        private static MultipartFormDataContent BuildUploadForm(
            string csrfToken,
            string fileName = "test.png",
            string contentType = "image/png",
            int sizeBytes = 128)
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(csrfToken), "__RequestVerificationToken");

            var fileContent = new ByteArrayContent(new byte[sizeBytes]);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            return content;
        }

        private static MultipartFormDataContent BuildDeleteForm(string csrfToken)
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(csrfToken), "__RequestVerificationToken");
            return content;
        }

        /// <summary>
        /// Uploads a file and follows the redirect to the Details page.
        /// Returns the HTML of the Details page after upload.
        /// </summary>
        private async Task<string> UploadAndGetDetailsHtmlAsync(
            HttpClient client, Guid noteId, string fileName, string contentType = "image/png", int sizeBytes = 128)
        {
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={noteId}");
            var form = BuildUploadForm(token!, fileName, contentType, sizeBytes);
            var uploadResponse = await client.PostAsync(
                $"/Notes/Details?id={noteId}&handler=Upload", form);

            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

            // Follow redirect to get updated page
            var detailsResponse = await client.GetAsync(uploadResponse.Headers.Location);
            return await detailsResponse.Content.ReadAsStringAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Details Page – Display Tests
        // ─────────────────────────────────────────────────────────────────────

        #region Details Page Display

        [Fact]
        public async Task Details_Page_Shows_Attachments_Section_And_NoAttachments_Message()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Display test");
            using var client = CreateClient();

            var response = await client.GetAsync($"/Notes/Details?id={note.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Attachments");
            html.Should().Contain("No attachments.");
        }

        [Fact]
        public async Task Details_Page_Shows_Upload_Form_With_Enctype()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Form test");
            using var client = CreateClient();

            var response = await client.GetAsync($"/Notes/Details?id={note.Id}");
            var html = await response.Content.ReadAsStringAsync();

            html.Should().Contain("enctype=\"multipart/form-data\"");
            html.Should().Contain("Upload");
        }

        [Fact]
        public async Task Details_For_NonexistentNote_Returns_NotFound()
        {
            using var client = CreateClient();
            var response = await client.GetAsync($"/Notes/Details?id={Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        // Upload Attachment – Positive Tests
        // ─────────────────────────────────────────────────────────────────────

        #region Upload – Positive

        [Fact]
        public async Task Upload_NewAttachment_RedirectsToDetailsWithSuccessMessage()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Upload test");
            using var client = CreateClient();

            var html = await UploadAndGetDetailsHtmlAsync(client, note.Id, "MilkAndEggs.png");

            html.Should().Contain("uploaded successfully");
            html.Should().Contain("MilkAndEggs.png");
        }

        [Fact]
        public async Task Upload_MultipleAttachments_AllShowOnDetailsPage()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Multi-upload test");
            using var client = CreateClient();

            await UploadAndGetDetailsHtmlAsync(client, note.Id, "File1.png");
            await UploadAndGetDetailsHtmlAsync(client, note.Id, "File2.pdf", "application/pdf");
            var html = await UploadAndGetDetailsHtmlAsync(client, note.Id, "File3.jpg", "image/jpeg");

            html.Should().Contain("File1.png");
            html.Should().Contain("File2.pdf");
            html.Should().Contain("File3.jpg");
            html.Should().NotContain("No attachments.");
        }

        [Fact]
        public async Task Upload_UpToMaxAttachments_AllSucceed()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Max limit test");
            using var client = CreateClient();

            // Upload 3 (the maximum)
            string[] files = { "Chocolate.png", "Diamonds.png", "NewCar.png" };
            foreach (var file in files)
            {
                var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={note.Id}");
                var form = BuildUploadForm(token!, file);
                var response = await client.PostAsync(
                    $"/Notes/Details?id={note.Id}&handler=Upload", form);
                response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                    $"Upload of '{file}' should succeed");
            }

            // Verify all 3 are shown
            var detailsResponse = await client.GetAsync($"/Notes/Details?id={note.Id}");
            var html = await detailsResponse.Content.ReadAsStringAsync();
            html.Should().Contain("Chocolate.png");
            html.Should().Contain("Diamonds.png");
            html.Should().Contain("NewCar.png");
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        // Upload Attachment – Negative Tests
        // ─────────────────────────────────────────────────────────────────────

        #region Upload – Negative

        [Fact]
        public async Task Upload_NoFile_ReturnsPageWithValidationMessage()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "No file test");
            using var client = CreateClient();

            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={note.Id}");
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(token!), "__RequestVerificationToken");

            var response = await client.PostAsync(
                $"/Notes/Details?id={note.Id}&handler=Upload", content);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Please select a file");
        }

        [Fact]
        public async Task Upload_ExceedsMaxAttachments_ReturnsPageWithLimitMessage()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Exceed limit test");
            using var client = CreateClient();

            // Fill to the limit (3)
            for (int i = 1; i <= 3; i++)
            {
                var (_, t) = await GetWithCsrfAsync(client, $"/Notes/Details?id={note.Id}");
                var f = BuildUploadForm(t!, $"file{i}.png");
                await client.PostAsync($"/Notes/Details?id={note.Id}&handler=Upload", f);
            }

            // Fourth upload should be rejected
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={note.Id}");
            var form = BuildUploadForm(token!, "extra.png");
            var response = await client.PostAsync(
                $"/Notes/Details?id={note.Id}&handler=Upload", form);

            // The handler returns Page() (200), not redirect, when limit is hit
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Maximum of");
        }

        [Fact]
        public async Task Upload_ToOtherUsersNote_ReturnsNotFound()
        {
            var otherNote = await _factory.SeedNoteAsync(OtherUserId, summary: "Other user note");
            var ownNote = await _factory.SeedNoteAsync(UserId, summary: "My note for token");
            using var client = CreateClient();

            // Get a CSRF token from our own note's details page
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={ownNote.Id}");
            var form = BuildUploadForm(token!, "sneaky.png");
            var response = await client.PostAsync(
                $"/Notes/Details?id={otherNote.Id}&handler=Upload", form);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        // Download Attachment Tests
        // ─────────────────────────────────────────────────────────────────────

        #region Download

        [Fact]
        public async Task Download_ExistingAttachment_ReturnsFileContent()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Download test");
            using var client = CreateClient();

            // Upload first
            await UploadAndGetDetailsHtmlAsync(client, note.Id, "TestDoc.png");

            // Get the blob name from the details page
            var detailsResponse = await client.GetAsync($"/Notes/Details?id={note.Id}");
            var html = await detailsResponse.Content.ReadAsStringAsync();

            // Extract the blob name (GUID) from the download link
            var blobMatch = Regex.Match(html, @"attachmentId=([a-f0-9\-]{36})", RegexOptions.IgnoreCase);
            blobMatch.Success.Should().BeTrue("Details page should contain a download link with attachmentId");
            var blobName = blobMatch.Groups[1].Value;

            // Download
            var downloadResponse = await client.GetAsync(
                $"/Notes/Details?id={note.Id}&handler=Download&attachmentId={blobName}");

            downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            downloadResponse.Content.Headers.ContentType.Should().NotBeNull();
        }

        [Fact]
        public async Task Download_NonexistentAttachment_ReturnsNotFound()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Download missing test");
            using var client = CreateClient();

            var response = await client.GetAsync(
                $"/Notes/Details?id={note.Id}&handler=Download&attachmentId={Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Download_OtherUsersNote_ReturnsNotFound()
        {
            var otherNote = await _factory.SeedNoteAsync(OtherUserId, summary: "Other user download");
            using var client = CreateClient();

            var response = await client.GetAsync(
                $"/Notes/Details?id={otherNote.Id}&handler=Download&attachmentId={Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        // Delete Attachment Tests
        // ─────────────────────────────────────────────────────────────────────

        #region Delete

        [Fact]
        public async Task Delete_ExistingAttachment_RedirectsWithSuccessMessage()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Delete test");
            using var client = CreateClient();

            // Upload an attachment
            await UploadAndGetDetailsHtmlAsync(client, note.Id, "ToDelete.png");

            // Get the blob name from the details page
            var detailsResponse = await client.GetAsync($"/Notes/Details?id={note.Id}");
            var html = await detailsResponse.Content.ReadAsStringAsync();
            var blobMatch = Regex.Match(html, @"attachmentId=([a-f0-9\-]{36})", RegexOptions.IgnoreCase);
            blobMatch.Success.Should().BeTrue();
            var blobName = blobMatch.Groups[1].Value;

            // Delete the attachment
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={note.Id}");
            var deleteForm = BuildDeleteForm(token!);
            var deleteResponse = await client.PostAsync(
                $"/Notes/Details?id={note.Id}&handler=Delete&attachmentId={blobName}", deleteForm);

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

            // Follow redirect and verify it's gone
            var afterDeleteResponse = await client.GetAsync(deleteResponse.Headers.Location);
            var afterHtml = await afterDeleteResponse.Content.ReadAsStringAsync();
            afterHtml.Should().Contain("deleted successfully");
            afterHtml.Should().Contain("No attachments.");
        }

        [Fact]
        public async Task Delete_NonexistentAttachment_RedirectsWithNotFoundMessage()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Delete missing test");
            using var client = CreateClient();

            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={note.Id}");
            var form = BuildDeleteForm(token!);
            var response = await client.PostAsync(
                $"/Notes/Details?id={note.Id}&handler=Delete&attachmentId={Guid.NewGuid()}", form);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            // Follow redirect and verify the status message
            var afterResponse = await client.GetAsync(response.Headers.Location);
            var html = await afterResponse.Content.ReadAsStringAsync();
            html.Should().Contain("not found");
        }

        [Fact]
        public async Task Delete_OtherUsersNote_ReturnsNotFound()
        {
            var otherNote = await _factory.SeedNoteAsync(OtherUserId, summary: "Other user delete");
            var ownNote = await _factory.SeedNoteAsync(UserId, summary: "My note for token");
            using var client = CreateClient();

            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={ownNote.Id}");
            var form = BuildDeleteForm(token!);
            var response = await client.PostAsync(
                $"/Notes/Details?id={otherNote.Id}&handler=Delete&attachmentId={Guid.NewGuid()}", form);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        // Full Lifecycle Test
        // ─────────────────────────────────────────────────────────────────────

        #region Full Lifecycle

        /// <summary>
        /// Full lifecycle: create note → upload attachment → verify on page →
        /// download attachment → delete attachment → verify gone.
        /// </summary>
        [Fact]
        public async Task AttachmentLifecycle_UploadDownloadDelete_WorksCorrectly()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Lifecycle test");
            using var client = CreateClient();

            // ── UPLOAD ────────────────────────────────────────────────
            var html = await UploadAndGetDetailsHtmlAsync(client, note.Id, "LifecycleFile.pdf", "application/pdf");
            html.Should().Contain("uploaded successfully");
            html.Should().Contain("LifecycleFile.pdf");

            // ── VERIFY LISTED ─────────────────────────────────────────
            html.Should().NotContain("No attachments.");
            var blobMatch = Regex.Match(html, @"attachmentId=([a-f0-9\-]{36})", RegexOptions.IgnoreCase);
            blobMatch.Success.Should().BeTrue();
            var blobName = blobMatch.Groups[1].Value;

            // ── DOWNLOAD ──────────────────────────────────────────────
            var downloadResponse = await client.GetAsync(
                $"/Notes/Details?id={note.Id}&handler=Download&attachmentId={blobName}");
            downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            downloadedBytes.Length.Should().BeGreaterThan(0);

            // ── DELETE ────────────────────────────────────────────────
            var (_, token) = await GetWithCsrfAsync(client, $"/Notes/Details?id={note.Id}");
            var deleteForm = BuildDeleteForm(token!);
            var deleteResponse = await client.PostAsync(
                $"/Notes/Details?id={note.Id}&handler=Delete&attachmentId={blobName}", deleteForm);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

            // ── VERIFY GONE ───────────────────────────────────────────
            var afterResponse = await client.GetAsync($"/Notes/Details?id={note.Id}");
            var afterHtml = await afterResponse.Content.ReadAsStringAsync();
            afterHtml.Should().Contain("No attachments.");

            // Download should now return not found
            var reDownload = await client.GetAsync(
                $"/Notes/Details?id={note.Id}&handler=Download&attachmentId={blobName}");
            reDownload.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion
    }
}
