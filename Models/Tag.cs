using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HW5NoteKeeperSolution.Models
{
    /// <summary>
    /// Represents an AI-generated keyword tag that is associated with a <see cref="Note"/>.
    /// </summary>
    [Table("Tag")]
    public class Tag
    {
        /// <summary>Gets or sets the unique identifier for this tag.</summary>
        [Key]
        [Column("Id")]
        public Guid Id { get; set; }

        /// <summary>Gets or sets the identifier of the note this tag belongs to.</summary>
        [Required]
        [Column("NoteId")]
        public Guid NoteId { get; set; }

        /// <summary>Gets or sets the tag keyword (1–30 characters, lowercase).</summary>
        [Required]
        [StringLength(30, MinimumLength = 1)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the navigation property back to the owning note.</summary>
        [ForeignKey(nameof(NoteId))]
        public Note Note { get; set; } = null!;
    }
}
