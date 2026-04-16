using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;
using HW5NoteKeeper.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeper.Pages.Notes
{
    /// <summary>
    /// Razor Page model for the Note Details page.
    /// Displays note information, tags, and attachments.
    /// Supports uploading, downloading, and deleting attachments.
    /// Enforces multi-tenant ownership — returns 404 for notes belonging to other users.
    /// </summary>
    public class DetailsModel : NoteKeeperBasePageModel
    {
        private readonly IAzureStorageService _storageService;
        private readonly NoteLimits _noteLimits;
        private readonly ILogger<DetailsModel> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DetailsModel"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        /// <param name="storageService">The Azure Blob Storage service for attachment operations.</param>
        /// <param name="noteLimits">Application-level limits (e.g., max attachments per note).</param>
        /// <param name="logger">Logger for informational and error messages.</param>
        public DetailsModel(
            NoteKeeperContext context,
            IAzureStorageService storageService,
            NoteLimits noteLimits,
            ILogger<DetailsModel> logger) : base(context)
        {
            _storageService = storageService;
            _noteLimits = noteLimits;
            _logger = logger;
        }

        /// <summary>Gets or sets the note to display.</summary>
        public Note Note { get; set; } = default!;

        /// <summary>Gets or sets the list of attachments for the note.</summary>
        public List<AttachmentInfo> Attachments { get; set; } = new();

        /// <summary>Gets or sets a status message to display after an attachment operation.</summary>
        [TempData]
        public string? StatusMessage { get; set; }

        /// <summary>
        /// Handles GET requests. Loads the note with its tags and attachment list,
        /// enforcing multi-tenant ownership. Returns 404 when the note does not
        /// exist or belongs to another user.
        /// </summary>
        /// <param name="id">The unique identifier of the note to display.</param>
        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await Context.Notes
                .Include(n => n.Tags)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());

            if (note == null)
            {
                return NotFound();
            }

            Note = note;
            Attachments = await _storageService.ListAttachmentsAsync(note.Id.ToString());
            return Page();
        }

        /// <summary>
        /// Handles POST requests to upload an attachment file to Azure Blob Storage.
        /// Validates the file and enforces the maximum attachments limit.
        /// </summary>
        /// <param name="id">The note ID to attach the file to.</param>
        /// <param name="file">The uploaded file.</param>
        public async Task<IActionResult> OnPostUploadAsync(Guid id, IFormFile? file)
        {
            var note = await Context.Notes
                .Include(n => n.Tags)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserRealmId == User.GetObjectIdentifier());

            if (note == null)
            {
                return NotFound();
            }

            Note = note;

            if (file == null || file.Length == 0)
            {
                StatusMessage = "Please select a file to upload.";
                Attachments = await _storageService.ListAttachmentsAsync(note.Id.ToString());
                return Page();
            }

            // Enforce attachment limit
            int currentCount = await _storageService.GetBlobCountAsync(note.Id.ToString());
            if (currentCount >= _noteLimits.MaxAttachments)
            {
                StatusMessage = $"Maximum of {_noteLimits.MaxAttachments} attachments per note has been reached.";
                Attachments = await _storageService.ListAttachmentsAsync(note.Id.ToString());
                return Page();
            }

            string attachmentId = Guid.NewGuid().ToString();
            await _storageService.UploadAttachmentAsync(note.Id.ToString(), attachmentId, file);

            _logger.LogInformation(
                "Uploaded attachment {AttachmentId} (original: {OriginalFileName}) to note {NoteId}",
                attachmentId, file.FileName, note.Id);

            StatusMessage = $"File '{file.FileName}' uploaded successfully.";
            return RedirectToPage(new { id });
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
        /// Redirects back to the Details page after deletion.
        /// </summary>
        /// <param name="id">The note ID that owns the attachment.</param>
        /// <param name="attachmentId">The blob name (GUID) of the attachment to delete.</param>
        public async Task<IActionResult> OnPostDeleteAsync(Guid id, string attachmentId)
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

            return RedirectToPage(new { id });
        }
    }
}
