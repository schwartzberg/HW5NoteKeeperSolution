using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HW5NoteKeeperSolution.Models
{
    [Table("NoteMultiTenant")]
    public class Note
    {
        [Key]
        [Column("Id")]
        public Guid Id { get; set; }

        [Required]
        [StringLength(60, MinimumLength = 1)]
        [Column("summary")]
        public string Summary { get; set; } = string.Empty;

        [Required]
        [StringLength(1024, MinimumLength = 1)]
        [Column("details")]
        public string Details { get; set; } = string.Empty;

        [Required]
        [Column("CreatedDateUtc")]
        public DateTimeOffset CreatedDateUtc { get; set; }

        [Column("ModifiedDateUtc")]
        public DateTimeOffset? ModifiedDateUtc { get; set; }

        [Column("UserRealmId")]
        public string? UserRealmId { get; set; }

        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
