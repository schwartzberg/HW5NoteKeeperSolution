using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HW5NoteKeeperSolution.Models
{
    /// <summary>
    /// Represents a multi-tenant note stored in the <c>NoteMultiTenant</c> table.
    /// Each note belongs to a single user identified by <see cref="UserRealmId"/>.
    /// </summary>
    [Table("NoteMultiTenant")]
    public class Note
    {
        /// <summary>Gets or sets the unique identifier for this note.</summary>
        [Key]
        [Column("Id")]
        public Guid Id { get; set; }

        /// <summary>Gets or sets a short summary of the note (1–60 characters).</summary>
        [Required]
        [StringLength(60, MinimumLength = 1)]
        [Column("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>Gets or sets the full detail text of the note (1–1024 characters).</summary>
        [Required]
        [StringLength(1024, MinimumLength = 1)]
        [Column("details")]
        public string Details { get; set; } = string.Empty;

        /// <summary>Gets or sets the UTC timestamp when the note was created.</summary>
        [Required]
        [Column("CreatedDateUtc")]
        public DateTimeOffset CreatedDateUtc { get; set; }

        /// <summary>Gets or sets the UTC timestamp of the most recent edit, or <see langword="null"/> if never edited.</summary>
        [Column("ModifiedDateUtc")]
        public DateTimeOffset? ModifiedDateUtc { get; set; }

        /// <summary>Gets or sets the Entra object identifier of the user who owns this note.</summary>
        [Column("UserRealmId")]
        public string? UserRealmId { get; set; }

        /// <summary>Gets or sets the collection of AI-generated tags associated with this note.</summary>
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
