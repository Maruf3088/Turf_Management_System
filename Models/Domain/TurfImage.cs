using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public class TurfImage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TurfId { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsMain { get; set; } = false;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("TurfId")]
        public Turf Turf { get; set; } = null!;
    }
}
