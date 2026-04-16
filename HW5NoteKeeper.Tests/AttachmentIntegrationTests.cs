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
        public async Task Upload_MultipleAttachments_AllSucceed()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Multi upload test");
            using var client = CreateClient();

            // Upload 3 files
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

        // ─────────────────────────────────────────────────────────────────────
        // Index Page – Attachment Tests
        // ─────────────────────────────────────────────────────────────────────

        #region Index Page Attachments

        /// <summary>
        /// Uploads a file via the Index page handler.
        /// Returns the redirect response (does NOT follow it).
        /// </summary>
        private async Task<HttpResponseMessage> UploadViaIndexAsync(
            HttpClient client, Guid noteId, string fileName,
            string contentType = "image/png", int sizeBytes = 128)
        {
            var (_, token) = await GetWithCsrfAsync(client, "/Notes");
            var form = BuildUploadForm(token!, fileName, contentType, sizeBytes);
            return await client.PostAsync(
                $"/Notes?id={noteId}&handler=Upload", form);
        }

        [Fact]
        public async Task IndexPage_Shows_Attachments_Column_Header()
        {
            await _factory.SeedNoteAsync(UserId, summary: "Index col test");
            using var client = CreateClient();

            var response = await client.GetAsync("/Notes");
            var html = await response.Content.ReadAsStringAsync();

            html.Should().Contain("Attachments");
        }

        [Fact]
        public async Task IndexPage_Upload_Attachment_Redirects_And_ShowsFile()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Index upload test");
            using var client = CreateClient();

            var uploadResponse = await UploadViaIndexAsync(client, note.Id, "index-file.txt");
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

            // Follow redirect with same client (TempData needs the cookie)
            var indexResponse = await client.GetAsync(uploadResponse.Headers.Location);
            var html = await indexResponse.Content.ReadAsStringAsync();

            html.Should().Contain("index-file.txt");
            html.Should().Contain("uploaded successfully");
        }

        [Fact]
        public async Task IndexPage_Download_Attachment_ReturnsFile()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Index download test");
            using var client = CreateClient();

            // Upload first
            await UploadViaIndexAsync(client, note.Id, "download-me.pdf", "application/pdf", 256);

            // Get the Index page and find the download link
            var indexResponse = await client.GetAsync("/Notes");
            var html = await indexResponse.Content.ReadAsStringAsync();

            var downloadMatch = Regex.Match(html,
                @"handler=Download[^""]*id=" + note.Id + @"[^""]*attachmentId=([a-f0-9\-]+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!downloadMatch.Success)
            {
                downloadMatch = Regex.Match(html,
                    @"attachmentId=([a-f0-9\-]+)[^""]*handler=Download",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            downloadMatch.Success.Should().BeTrue("download link should exist on Index page");
            var blobName = downloadMatch.Groups[1].Value;

            // Download
            var downloadResponse = await client.GetAsync(
                $"/Notes?id={note.Id}&handler=Download&attachmentId={blobName}");
            downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            downloadResponse.Content.Headers.ContentDisposition!.FileName.Should().Contain("download-me.pdf");
        }

        [Fact]
        public async Task IndexPage_Delete_Attachment_RemovesIt()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Index delete test");
            using var client = CreateClient();

            // Upload
            await UploadViaIndexAsync(client, note.Id, "to-delete.txt", "text/plain");

            // Get page to find the blob name via the delete form
            var indexHtml = await (await client.GetAsync("/Notes")).Content.ReadAsStringAsync();

            var blobMatch = Regex.Match(indexHtml,
                @"attachmentId=([a-f0-9\-]+)[^""]*handler=DeleteAttachment",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!blobMatch.Success)
            {
                blobMatch = Regex.Match(indexHtml,
                    @"handler=DeleteAttachment[^""]*attachmentId=([a-f0-9\-]+)",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            blobMatch.Success.Should().BeTrue("delete form should exist on Index page");
            var blobName = blobMatch.Groups[1].Value;

            // Delete
            var (_, token) = await GetWithCsrfAsync(client, "/Notes");
            var deleteForm = BuildDeleteForm(token!);
            var deleteResponse = await client.PostAsync(
                $"/Notes?id={note.Id}&handler=DeleteAttachment&attachmentId={blobName}", deleteForm);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

            // Verify it's gone
            var afterResponse = await client.GetAsync("/Notes");
            var afterHtml = await afterResponse.Content.ReadAsStringAsync();
            afterHtml.Should().Contain("deleted successfully");
            afterHtml.Should().NotContain("to-delete.txt");
        }

        [Fact]
        public async Task IndexPage_Upload_NoFile_ShowsError()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "No file test");
            using var client = CreateClient();

            // POST with no file content
            var (_, token) = await GetWithCsrfAsync(client, "/Notes");
            var emptyForm = new MultipartFormDataContent();
            emptyForm.Add(new StringContent(token!), "__RequestVerificationToken");

            var response = await client.PostAsync(
                $"/Notes?id={note.Id}&handler=Upload", emptyForm);
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            // Follow redirect with same client to see TempData message
            var indexResponse = await client.GetAsync(response.Headers.Location);
            var html = await indexResponse.Content.ReadAsStringAsync();

            html.Should().Contain("select a file");
        }

        [Fact]
        public async Task IndexPage_OtherUser_Cannot_Upload_Attachment()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Other user upload test");
            using var otherClient = CreateClient(OtherUserId);

            // Get CSRF token from Create page (other user's Index has no forms)
            var (_, token) = await GetWithCsrfAsync(otherClient, "/Notes/Create");
            token.Should().NotBeNull();
            var form = BuildUploadForm(token!, "hack.txt");
            var uploadResponse = await otherClient.PostAsync(
                $"/Notes?id={note.Id}&handler=Upload", form);

            // Should be NotFound (ownership check fails)
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task IndexPage_OtherUser_Cannot_Download_Attachment()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Other user download test");
            using var ownerClient = CreateClient();

            // Owner uploads
            await UploadViaIndexAsync(ownerClient, note.Id, "secret.txt");

            // Get blob name
            var html = await (await ownerClient.GetAsync("/Notes")).Content.ReadAsStringAsync();
            var blobMatch = Regex.Match(html,
                @"attachmentId=([a-f0-9\-]+)",
                RegexOptions.IgnoreCase);
            blobMatch.Success.Should().BeTrue();
            var blobName = blobMatch.Groups[1].Value;

            // Other user tries to download
            using var otherClient = CreateClient(OtherUserId);
            var response = await otherClient.GetAsync(
                $"/Notes?id={note.Id}&handler=Download&attachmentId={blobName}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task IndexPage_OtherUser_Cannot_Delete_Attachment()
        {
            var note = await _factory.SeedNoteAsync(UserId, summary: "Other user delete test");
            using var ownerClient = CreateClient();

            // Owner uploads
            await UploadViaIndexAsync(ownerClient, note.Id, "protected.txt");

            // Get blob name
            var html = await (await ownerClient.GetAsync("/Notes")).Content.ReadAsStringAsync();
            var blobMatch = Regex.Match(html,
                @"attachmentId=([a-f0-9\-]+)",
                RegexOptions.IgnoreCase);
            blobMatch.Success.Should().BeTrue();
            var blobName = blobMatch.Groups[1].Value;

            // Other user tries to delete — get CSRF from Create page
            using var otherClient = CreateClient(OtherUserId);
            var (_, token) = await GetWithCsrfAsync(otherClient, "/Notes/Create");
            var deleteForm = BuildDeleteForm(token!);
            var response = await otherClient.PostAsync(
                $"/Notes?id={note.Id}&handler=DeleteAttachment&attachmentId={blobName}", deleteForm);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion
    }
}
