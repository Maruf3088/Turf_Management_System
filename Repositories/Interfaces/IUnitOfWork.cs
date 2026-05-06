using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IGenericRepository<Role> Roles { get; }
        Task<int> CompleteAsync();
    }
}
