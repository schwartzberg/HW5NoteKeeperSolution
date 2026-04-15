using Microsoft.EntityFrameworkCore;
using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Data
{
    public class NoteKeeperContext : DbContext
    {
        public NoteKeeperContext(DbContextOptions<NoteKeeperContext> options)
            : base(options)
        {
        }

        public DbSet<Note> Notes { get; set; } = default!;

        public DbSet<Tag> Tags { get; set; } = default!;

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
