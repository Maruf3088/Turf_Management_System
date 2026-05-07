using System.ComponentModel.DataAnnotations;
using turf_management_system.Models.Domain;
using turf_management_system.Models.Pagination;

namespace turf_management_system.Models.ViewModels
{
    public class RoleListVM
    {
        public PagedResult<Role> PagedRoles { get; set; } = new PagedResult<Role>();
        public string? SearchTerm { get; set; }
    }

    public class RoleVM
    {
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Role Name is required")]
        [StringLength(50, ErrorMessage = "Role Name cannot exceed 50 characters")]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;
    }
}
