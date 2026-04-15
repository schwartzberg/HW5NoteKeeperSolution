using Microsoft.EntityFrameworkCore;
using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Data
{
    /// <summary>
    /// EF Core <c>DbContext</c> for the NoteKeeper application.
    /// Manages the <see cref="Notes"/> and <see cref="Tags"/> tables and configures their relationships.
    /// </summary>
    public class NoteKeeperContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NoteKeeperContext"/> with the given options.
        /// </summary>
        /// <param name="options">The EF Core context options (provider, connection string, etc.).</param>
        public NoteKeeperContext(DbContextOptions<NoteKeeperContext> options)
            : base(options)
        {
        }

        /// <summary>Gets or sets the <see cref="Note"/> entities (mapped to the <c>NoteMultiTenant</c> table).</summary>
        public DbSet<Note> Notes { get; set; } = default!;

        /// <summary>Gets or sets the <see cref="Tag"/> entities (mapped to the <c>Tag</c> table).</summary>
        public DbSet<Tag> Tags { get; set; } = default!;

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>().ToTable("Tag");

            modelBuilder.Entity<Note>()
                .ToTable("NoteMultiTenant")
                .HasMany(note => note.Tags)
                .WithOne(tag => tag.Note)
                .HasForeignKey(tag => tag.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Note>()
                .HasIndex(note => note.UserRealmId);
        }
    }
}
