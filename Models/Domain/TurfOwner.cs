using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public class TurfOwner
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? BusinessAddress { get; set; }

        [StringLength(20)]
        public string? ContactNumber { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}
