using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace HW5NoteKeeperSolution.Services
{
    /// <summary>
    /// Creates the four default seed notes (with tags and blob attachments) for a new user
    /// on their first authenticated request. Uses a per-user lock and an in-memory cache
    /// to guarantee idempotency under concurrent requests.
    /// </summary>
    public class UserNoteSeedService : IUserNoteSeedService
    {
        private static readonly SeedNoteDefinition[] SeedNotes =
        [
            new("Running grocery list", "Milk, Eggs, Oranges"),
            new("Gift supplies notes", "Tape & Wrapping Paper"),
            new("Valentine's Day gift ideas", "Chocolate, Diamonds, New car"),
            new("Azure tips", "portal.azure.com is a quick way to get to the portal. Remember double underscore for Linux and colon for windows")
        ];

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserLocks = new(StringComparer.Ordinal);

        private readonly NoteKeeperContext _context;
        private readonly INoteTagService _noteTagService;
        private readonly IAzureStorageInitializer _storageInitializer;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<UserNoteSeedService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="UserNoteSeedService"/>.
        /// </summary>
        /// <param name="context">EF Core context used to query and persist seed notes and tags.</param>
        /// <param name="noteTagService">Service that generates and attaches AI tags to each seed note.</param>
        /// <param name="storageInitializer">Service that creates blob containers and uploads seed attachments.</param>
        /// <param name="memoryCache">In-memory cache used to skip re-seeding for users already seeded in this process lifetime.</param>
        /// <param name="logger">Logger for seeding progress and completion messages.</param>
        public UserNoteSeedService(
            NoteKeeperContext context,
            INoteTagService noteTagService,
            IAzureStorageInitializer storageInitializer,
            IMemoryCache memoryCache,
            ILogger<UserNoteSeedService> logger)
        {
            _context = context;
            _noteTagService = noteTagService;
            _storageInitializer = storageInitializer;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task EnsureSeedDataAsync(string userRealmId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userRealmId))
            {
                throw new InvalidOperationException("A user object identifier is required for per-user note seeding.");
            }

            string cacheKey = $"seeded-user:{userRealmId}";
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                return;
            }

            SemaphoreSlim userLock = UserLocks.GetOrAdd(userRealmId, static _ => new SemaphoreSlim(1, 1));
            await userLock.WaitAsync(cancellationToken);

            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out _))
                {
                    return;
                }

                Dictionary<string, Note> notesBySummary = await _context.Notes
                    .Include(note => note.Tags)
                    .Where(note => note.UserRealmId == userRealmId)
                    .ToDictionaryAsync(note => note.Summary, StringComparer.OrdinalIgnoreCase, cancellationToken);

                foreach (SeedNoteDefinition seedNote in SeedNotes)
                {
                    if (!notesBySummary.TryGetValue(seedNote.Summary, out Note? note))
                    {
                        note = new Note
                        {
                            Id = Guid.NewGuid(),
                            Summary = seedNote.Summary,
                            Details = seedNote.Details,
                            CreatedDateUtc = DateTimeOffset.UtcNow,
                            UserRealmId = userRealmId
                        };

                        await _noteTagService.ApplyGeneratedTagsAsync(note, cancellationToken: cancellationToken);
                        _context.Notes.Add(note);
                        notesBySummary[seedNote.Summary] = note;
                    }
                    else if (note.Tags.Count == 0)
                    {
                        await _noteTagService.ApplyGeneratedTagsAsync(note, cancellationToken: cancellationToken);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                foreach (SeedNoteDefinition seedNote in SeedNotes)
                {
                    Note note = notesBySummary[seedNote.Summary];
                    bool seeded = await _storageInitializer.InitializeAsync(
                        note.Id,
                        note.Summary,
                        cancellationToken: cancellationToken);

                    if (!seeded)
                    {
                        throw new InvalidOperationException($"Attachment seeding failed for note '{note.Summary}'.");
                    }
                }

                _memoryCache.Set(cacheKey, true);
                _logger.LogInformation("Completed first-login seed data for user realm {UserRealmId}.", userRealmId);
            }
            finally
            {
                userLock.Release();
            }
        }

        private sealed record SeedNoteDefinition(string Summary, string Details);
    }
}
