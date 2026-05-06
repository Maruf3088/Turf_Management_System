using System.ComponentModel.DataAnnotations;

namespace turf_management_system.Models.Domain
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
