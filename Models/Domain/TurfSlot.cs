using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public class TurfSlot
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TurfId { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int? DayOfWeek { get; set; } // 0=Sunday to 6=Saturday; null means all days

        // Navigation property
        [ForeignKey("TurfId")]
        public Turf Turf { get; set; } = null!;
    }
}
