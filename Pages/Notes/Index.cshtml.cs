using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeper.Pages.Notes
{
    /// <summary>
    /// Razor Page model for the Notes list (Index) page.
    /// Displays only the notes that belong to the authenticated user, ordered by most recently created.
    /// Seeds default notes on first visit when the user has none.
    /// Supports inline attachment upload, download, and delete per note.
    /// </summary>
    public class IndexModel : NoteKeeperBasePageModel
    {
        private readonly IUserNoteSeedService _seedService;
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<IndexModel> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="IndexModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        /// <param name="seedService">Service that seeds default notes for first-time users.</param>
        /// <param name="storageService">The Azure Blob Storage service for attachment operations.</param>
        /// <param name="logger">Logger for informational and error messages.</param>
        public IndexModel(
            NoteKeeperContext context,
            IUserNoteSeedService seedService,
            IAzureStorageService storageService,
            ILogger<IndexModel> logger) : base(context)
        {
            _seedService = seedService;
            _storageService = storageService;
            _logger = logger;
        }

        /// <summary>Gets or sets the list of notes owned by the current user.</summary>
        public IList<Note> Note { get; set; } = default!;

        /// <summary>Gets or sets attachments per note, keyed by note ID.</summary>
        public Dictionary<Guid, List<AttachmentInfo>> AttachmentsByNote { get; set; } = new();

        /// <summary>Gets or sets a status message to display after an attachment operation.</summary>
        [TempData]
        public string? StatusMessage { get; set; }

        /// <summary>
        /// Handles GET requests. Seeds default notes if the user has none, then loads all
        /// notes (with tags) belonging to the authenticated user, ordered by descending creation date.
        /// Also loads attachments for each note.
        /// </summary>
        public async Task OnGetAsync()
        {
            var userRealmId = User.GetObjectIdentifier();

            await _seedService.EnsureSeedDataAsync(userRealmId, HttpContext.RequestAborted);

            Note = await Context.Notes
                .Include(n => n.Tags)
                .Where(n => n.UserRealmId == userRealmId)
                .OrderByDescending(n => n.CreatedDateUtc)
                .ToListAsync();

            foreach (var note in Note)
            {
                var attachments = await _storageService.ListAttachmentsAsync(note.Id.ToString());
                AttachmentsByNote[note.Id] = attachments;
            }
        }

        /// <summary>
        /// Handles POST requests to upload an attachment file to Azure Blob Storage from the Index page.
        /// Validates the file and enforces the maximum attachments limit.
        /// </summary>
        /// <param name="id">The note ID to attach the file to.</param>
        /// <param name="file">The uploaded file.</param>
        public async Task<IActionResult> OnPostUploadAsync(Guid id, IFormFile? file)
        {
            var note = await Context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());

            if (note == null)
            {
                return NotFound();
            }

            if (file == null || file.Length == 0)
            {
                StatusMessage = "Please select a file to upload.";
                return RedirectToPage();
            }

            string attachmentId = Guid.NewGuid().ToString();
            await _storageService.UploadAttachmentAsync(note.Id.ToString(), attachmentId, file);

            _logger.LogInformation(
                "Uploaded attachment {AttachmentId} (original: {OriginalFileName}) to note {NoteId}",
                attachmentId, file.FileName, note.Id);

            StatusMessage = $"File '{file.FileName}' uploaded successfully.";
            return RedirectToPage();
        }

        /// <summary>
        /// Handles GET requests to download an attachment from Azure Blob Storage.
        /// Returns the file as a download with the original file name.
        /// </summary>
        /// <param name="id">The note ID that owns the attachment.</param>
        /// <param name="attachmentId">The blob name (GUID) of the attachment to download.</param>
        public async Task<IActionResult> OnGetDownloadAsync(Guid id, string attachmentId)
        {
            var note = await Context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());

            if (note == null)
            {
                return NotFound();
            }

            var result = await _storageService.DownloadAttachmentAsync(note.Id.ToString(), attachmentId);
            if (result == null)
            {
                return NotFound();
            }

            var (stream, contentType, originalFileName) = result.Value;
            return File(stream, contentType, originalFileName);
        }

        /// <summary>
        /// Handles POST requests to delete an attachment from Azure Blob Storage.
        /// Redirects back to the Index page after deletion.
        /// </summary>
        /// <param name="id">The note ID that owns the attachment.</param>
        /// <param name="attachmentId">The blob name (GUID) of the attachment to delete.</param>
        public async Task<IActionResult> OnPostDeleteAttachmentAsync(Guid id, string attachmentId)
        {
            var note = await Context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());

            if (note == null)
            {
                return NotFound();
            }

            var deleteResult = await _storageService.DeleteAttachmentAsync(note.Id.ToString(), attachmentId);

            StatusMessage = deleteResult switch
            {
                AttachmentDeleteResult.Deleted => "Attachment deleted successfully.",
                AttachmentDeleteResult.NotFound => "Attachment was not found.",
                AttachmentDeleteResult.Error => "An error occurred while deleting the attachment.",
                _ => "Unknown result."
            };

            _logger.LogInformation(
                "Delete attachment {AttachmentId} from note {NoteId}: {Result}",
                attachmentId, note.Id, deleteResult);

            return RedirectToPage();
        }
    }
}
