using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface ITurfRepository : IGenericRepository<Turf>
    {
        Task<(IEnumerable<Turf> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize, string? search, string? city, string? sportType, bool? isApproved);
        Task<IEnumerable<Turf>> GetTurfsByOwnerIdAsync(int ownerId);
        Task<Turf?> GetTurfWithDetailsAsync(Guid turfId);
        Task<IEnumerable<string>> GetDistinctCitiesAsync();
        Task<IEnumerable<string>> GetDistinctSportTypesAsync();
    }
}
