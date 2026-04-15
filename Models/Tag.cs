using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HW5NoteKeeperSolution.Models
{
    [Table("Tag")]
    public class Tag
    {
        [Key]
        [Column("Id")]
        public Guid Id { get; set; }

        [Required]
        [Column("NoteId")]
        public Guid NoteId { get; set; }

        [Required]
        [StringLength(30, MinimumLength = 1)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(NoteId))]
        public Note Note { get; set; } = null!;
    }
}
