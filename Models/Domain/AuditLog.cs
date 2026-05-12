using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty; // e.g. "Booking", "Payment"

        [Required]
        [StringLength(100)]
        public string EntityId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // e.g. "StatusChanged", "PaymentVerified"

        public string? OldValue { get; set; }  // JSON snapshot
        public string? NewValue { get; set; }  // JSON snapshot

        [StringLength(500)]
        public string? Notes { get; set; }

        public int? PerformedByUserId { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("PerformedByUserId")]
        public User? PerformedByUser { get; set; }
    }
}
