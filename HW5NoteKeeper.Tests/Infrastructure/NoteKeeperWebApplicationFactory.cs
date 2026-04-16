using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HW5NoteKeeper.Data;
using HW5NoteKeeper.Models;
using HW5NoteKeeper.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace HW5NoteKeeper.Tests.Infrastructure
{
    /// <summary>
    /// <see cref="WebApplicationFactory{TEntryPoint}"/> for Notes integration tests.
    /// Replaces all Azure-dependent services with in-memory / no-op equivalents and
    /// swaps the OIDC auth stack for <see cref="TestAuthHandler"/>.
    /// </summary>
    public sealed class NoteKeeperWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"NoteKeeperIntegrationTests-{Guid.NewGuid()}";

        // Dedicated EF service provider for InMemory. Using UseInternalServiceProvider
        // avoids the "multiple providers" error that occurs because Program.cs registers
        // AddEntityFrameworkSqlServer AND our factory registers AddEntityFrameworkInMemoryDatabase
        // into the same application DI container.
        private static readonly IServiceProvider _inMemoryEfServiceProvider =
            new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=(localdb)\\mssqllocaldb;Database=NoteKeeperTests;Trusted_Connection=True;",
                    ["AzureOpenAI:DeploymentUri"] = "https://test.openai.azure.com/",
                    ["AzureOpenAI:DeploymentModelName"] = "test-model",
                    ["StorageAccountSettings:Url"] = "https://test.blob.core.windows.net/",
                    ["StorageAccountSettings:TenantId"] = "test-tenant",
                    ["StorageAccountSettings:AccountName"] = "testaccount",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                // ── EF: swap SQL Server for InMemory ─────────────────────────────
                // Remove the SqlServer options descriptor, then register InMemory options
                // bound to our dedicated EF internal service provider so that EF doesn't
                // scan the application container (which has both SqlServer and InMemory
                // provider services) and throw "multiple providers" at runtime.
                ServiceDescriptor? efDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<NoteKeeperContext>));
                if (efDescriptor is not null)
                    services.Remove(efDescriptor);
                services.AddDbContext<NoteKeeperContext>(o =>
                    o.UseInMemoryDatabase(_dbName)
                     .UseInternalServiceProvider(_inMemoryEfServiceProvider));

                // ── Infrastructure stubs ─────────────────────────────────────────
                services.RemoveAll<IChatClient>();
                services.AddSingleton<IChatClient>(new Mock<IChatClient>().Object);

                services.RemoveAll<INoteTagService>();
                services.AddScoped<INoteTagService, TestNoteTagService>();

                services.RemoveAll<IUserNoteSeedService>();
                services.AddScoped<IUserNoteSeedService, NoOpUserNoteSeedService>();

                services.RemoveAll<IAzureStorageInitializer>();
                services.AddSingleton<IAzureStorageInitializer, NoOpAzureStorageInitializer>();

                services.RemoveAll<IAzureStorageService>();
                services.AddSingleton<IAzureStorageService, InMemoryAzureStorageService>();

                // Replace Azure Storage clients so DefaultAzureCredential is never resolved.
                services.RemoveAll<BlobServiceClient>();
                services.AddSingleton(new BlobServiceClient(new Uri("https://test.blob.core.windows.net/")));

                services.RemoveAll<QueueServiceClient>();
                services.AddSingleton(new QueueServiceClient(new Uri("https://test.queue.core.windows.net/")));

                // ── Auth: replace OIDC stack with TestAuthHandler ─────────────────
                services.AddAuthentication()
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });

                services.PostConfigure<AuthenticationOptions>(opts =>
                {
                    opts.DefaultScheme = TestAuthHandler.SchemeName;
                    opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    opts.DefaultForbidScheme = TestAuthHandler.SchemeName;
                    opts.DefaultSignInScheme = TestAuthHandler.SchemeName;
                    opts.DefaultSignOutScheme = TestAuthHandler.SchemeName;
                });
            });
        }

        /// <summary>Removes all notes and tags from the in-memory database.</summary>
        public async Task ClearDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<NoteKeeperContext>();
            ctx.Tags.RemoveRange(ctx.Tags);
            ctx.Notes.RemoveRange(ctx.Notes);
            await ctx.SaveChangesAsync();
        }

        /// <summary>Removes all blobs from the in-memory storage.</summary>
        public void ClearStorage()
        {
            var storage = Services.GetRequiredService<IAzureStorageService>() as InMemoryAzureStorageService;
            storage?.Clear();
        }

        /// <summary>Seeds a note (and optionally a "seed-tag") directly into the database.</summary>
        public async Task<Note> SeedNoteAsync(
            string userId,
            string summary = "Seed Note",
            string details = "Seed details",
            bool withTag = false)
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<NoteKeeperContext>();

            var note = new Note
            {
                Id = Guid.NewGuid(),
                Summary = summary,
                Details = details,
                CreatedDateUtc = DateTimeOffset.UtcNow,
                UserRealmId = userId,
            };

            ctx.Notes.Add(note);
            if (withTag)
                ctx.Tags.Add(new Tag { NoteId = note.Id, Name = "seed-tag" });

            await ctx.SaveChangesAsync();
            return note;
        }

        // ── Private no-op service implementations ────────────────────────────────

        private sealed class NoOpUserNoteSeedService : IUserNoteSeedService
        {
            public Task EnsureSeedDataAsync(
                string userRealmId,
                CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private sealed class NoOpAzureStorageInitializer : IAzureStorageInitializer
        {
            public IReadOnlyDictionary<string, string[]> AttachmentMapping =>
                new Dictionary<string, string[]>();

            public Task<bool> InitializeAsync(
                Guid noteId,
                string summary,
                string? attachmentsDirectory = null,
                CancellationToken cancellationToken = default)
                => Task.FromResult(true);
        }

        /// <summary>
        /// In-memory attachment storage service used during integration tests.
        /// Stores attachments in a thread-safe dictionary so upload/list/download/delete
        /// handlers can be tested without Azure Blob Storage.
        /// </summary>
        internal sealed class InMemoryAzureStorageService : IAzureStorageService
        {
            private readonly Dictionary<string, List<(string blobName, string originalFileName, string contentType, byte[] data)>> _containers = new();

            /// <summary>Removes all containers and blobs from the in-memory store.</summary>
            public void Clear() => _containers.Clear();

            public Task<bool> UploadAttachmentAsync(string noteId, string attachmentId, IFormFile fileData)
            {
                var key = noteId.ToLowerInvariant();
                if (!_containers.ContainsKey(key))
                    _containers[key] = new();

                bool isNew = !_containers[key].Any(a => a.blobName == attachmentId);

                using var ms = new MemoryStream();
                fileData.CopyTo(ms);
                _containers[key].RemoveAll(a => a.blobName == attachmentId);
                _containers[key].Add((attachmentId, fileData.FileName, fileData.ContentType, ms.ToArray()));

                return Task.FromResult(isNew);
            }

            public Task<AttachmentDeleteResult> DeleteAttachmentAsync(string noteId, string attachmentId)
            {
                var key = noteId.ToLowerInvariant();
                if (!_containers.TryGetValue(key, out var list))
                    return Task.FromResult(AttachmentDeleteResult.NotFound);

                int removed = list.RemoveAll(a => a.blobName == attachmentId);
                return Task.FromResult(removed > 0 ? AttachmentDeleteResult.Deleted : AttachmentDeleteResult.NotFound);
            }

            public Task<(Stream stream, string contentType, string originalFileName)?> DownloadAttachmentAsync(string noteId, string attachmentId)
            {
                var key = noteId.ToLowerInvariant();
                if (!_containers.TryGetValue(key, out var list))
                    return Task.FromResult<(Stream, string, string)?>(null);

                var item = list.FirstOrDefault(a => a.blobName == attachmentId);
                if (item == default)
                    return Task.FromResult<(Stream, string, string)?>(null);

                return Task.FromResult<(Stream, string, string)?>((new MemoryStream(item.data), item.contentType, item.originalFileName));
            }

            public Task<List<AttachmentInfo>> ListAttachmentsAsync(string noteId)
            {
                var key = noteId.ToLowerInvariant();
                if (!_containers.TryGetValue(key, out var list))
                    return Task.FromResult(new List<AttachmentInfo>());

                return Task.FromResult(list.Select(a => new AttachmentInfo
                {
                    BlobName = a.blobName,
                    OriginalFileName = a.originalFileName,
                    ContentType = a.contentType,
                    Length = a.data.Length
                }).ToList());
            }

            public Task<int> GetBlobCountAsync(string noteId)
            {
                var key = noteId.ToLowerInvariant();
                return Task.FromResult(_containers.TryGetValue(key, out var list) ? list.Count : 0);
            }

            public Task<bool> ContainerExistsAsync(string noteId)
            {
                return Task.FromResult(_containers.ContainsKey(noteId.ToLowerInvariant()));
            }
        }

        private sealed class NoOpAzureStorageService : IAzureStorageService
        {
            public Task<bool> UploadAttachmentAsync(string noteId, string attachmentId, IFormFile fileData)
                => Task.FromResult(true);

            public Task<AttachmentDeleteResult> DeleteAttachmentAsync(string noteId, string attachmentId)
                => Task.FromResult(AttachmentDeleteResult.Deleted);

            public Task<(Stream stream, string contentType, string originalFileName)?> DownloadAttachmentAsync(string noteId, string attachmentId)
                => Task.FromResult<(Stream, string, string)?>(null);

            public Task<List<AttachmentInfo>> ListAttachmentsAsync(string noteId)
                => Task.FromResult(new List<AttachmentInfo>());

            public Task<int> GetBlobCountAsync(string noteId)
                => Task.FromResult(0);

            public Task<bool> ContainerExistsAsync(string noteId)
                => Task.FromResult(false);
        }
    }
}
