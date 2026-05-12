using System.Linq.Expressions;
using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetByRoleIdAsync(int roleId);
    }
}
