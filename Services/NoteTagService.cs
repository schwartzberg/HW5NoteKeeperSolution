using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Services
{
    public class NoteTagService : INoteTagService
    {
        private readonly ITagGeneratorService _tagGeneratorService;

        public NoteTagService(ITagGeneratorService tagGeneratorService)
        {
            _tagGeneratorService = tagGeneratorService;
        }

        public async Task ApplyGeneratedTagsAsync(Note note, bool replaceExistingTags = true, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(note);

            if (note.Id == Guid.Empty)
            {
                note.Id = Guid.NewGuid();
            }

            KeyTagsResponse response = await _tagGeneratorService.GenerateTagsAsync(note.Details, cancellationToken);

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
                throw new InvalidOperationException("At least one generated tag is required when saving a note.");
            }
        }
    }
}
