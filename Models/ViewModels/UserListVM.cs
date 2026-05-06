using turf_management_system.Models.Domain;
using turf_management_system.Models.Pagination;

namespace turf_management_system.Models.ViewModels
{
    public class UserListVM
    {
        public PagedResult<User> PagedUsers { get; set; } = new PagedResult<User>();
        public string? SearchTerm { get; set; }
    }
}
