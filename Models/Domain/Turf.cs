using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public class Turf
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(300)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerHour { get; set; }

        [Required]
        [StringLength(100)]
        public string SportType { get; set; } = string.Empty; // e.g. Football, Cricket

        [StringLength(100)]
        public string? TurfSize { get; set; } // e.g. 5v5, 7v7

        public string? Amenities { get; set; }

        [StringLength(50)]
        public string? IndoorOutdoor { get; set; }

        [StringLength(20)]
        public string? ContactNumber { get; set; }

        public bool IsApproved { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public bool IsDraft { get; set; } = true;

        [Required]
        public int OwnerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("OwnerId")]
        public User Owner { get; set; } = null!;

        public ICollection<TurfImage> Images { get; set; } = new List<TurfImage>();
        public ICollection<TurfSlot> Slots { get; set; } = new List<TurfSlot>();
    }
}
