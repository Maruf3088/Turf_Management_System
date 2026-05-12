using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public class StaffProfile
    {
        [Key]
        public int UserId { get; set; }
        
        [Required]
        public Guid TurfId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime HiredAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        [ForeignKey("TurfId")]
        public Turf Turf { get; set; } = null!;
    }
}
