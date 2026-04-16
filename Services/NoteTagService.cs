using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Services
{
    /// <summary>
    /// Orchestrates AI tag generation and attaches the resulting tags to a <see cref="Note"/>.
    /// Normalizes tags to lowercase, trims whitespace, de-duplicates, and enforces a maximum
    /// length of 30 characters and a maximum count of 5 tags per note.
    /// If the AI tag generator is unavailable (e.g. invalid API key), the failure is logged
    /// and the note is left without generated tags so that the save can still succeed.
    /// </summary>
    public class NoteTagService : INoteTagService
    {
        private readonly ITagGeneratorService _tagGeneratorService;
        private readonly ILogger<NoteTagService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="NoteTagService"/>.
        /// </summary>
        /// <param name="tagGeneratorService">The AI service used to generate raw keyword tags from note details.</param>
        /// <param name="logger">Logger for tag generation diagnostics and failure warnings.</param>
        public NoteTagService(ITagGeneratorService tagGeneratorService, ILogger<NoteTagService> logger)
        {
            _tagGeneratorService = tagGeneratorService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task ApplyGeneratedTagsAsync(Note note, bool replaceExistingTags = true, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(note);

            if (note.Id == Guid.Empty)
            {
                note.Id = Guid.NewGuid();
            }

            KeyTagsResponse? response;
            try
            {
                response = await _tagGeneratorService.GenerateTagsAsync(note.Details, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "AI tag generation failed for note {NoteId}. " +
                    "The note will be saved without generated tags.",
                    note.Id);
                return;
            }

            IEnumerable<string> normalizedTags = response.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(tag => tag.Length > 30 ? tag[..30] : tag)
                .Take(5);

            if (replaceExistingTags)
            {
                note.Tags.Clear();
            }

            HashSet<string> existingTagNames = new(
                note.Tags.Select(tag => tag.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (string tagName in normalizedTags)
            {
                if (!existingTagNames.Add(tagName))
                {
                    continue;
                }

                note.Tags.Add(new Tag
                {
                    Id = Guid.NewGuid(),
                    NoteId = note.Id,
                    Name = tagName
                });
            }

            if (note.Tags.Count == 0)
            {
                _logger.LogWarning(
                    "AI tag generation returned no usable tags for note {NoteId}. " +
                    "The note will be saved without tags.",
                    note.Id);
            }
        }
    }
}
