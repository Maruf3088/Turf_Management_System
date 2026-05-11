using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class TurfRepository : GenericRepository<Turf>, ITurfRepository
    {
        public TurfRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Turf> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize, string? search, string? city, string? sportType, bool? isApproved)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Name.Contains(search) || t.Location.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(t => t.City == city);
            }

            if (!string.IsNullOrWhiteSpace(sportType))
            {
                query = query.Where(t => t.SportType == sportType);
            }

            if (isApproved.HasValue)
            {
                query = query.Where(t => t.IsApproved == isApproved.Value);
            }

            int totalCount = await query.CountAsync();
            var items = await query.Include(t => t.Images)
                                   .OrderByDescending(t => t.CreatedAt)
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Turf>> GetTurfsByOwnerIdAsync(int ownerId)
        {
            return await _dbSet.Include(t => t.Images)
                               .Where(t => t.OwnerId == ownerId)
                               .ToListAsync();
        }

        public async Task<Turf?> GetTurfWithDetailsAsync(Guid turfId)
        {
            return await _dbSet.Include(t => t.Images)
                               .Include(t => t.Slots)
                               .Include(t => t.Owner)
                               .FirstOrDefaultAsync(t => t.Id == turfId);
        }

        public async Task<IEnumerable<string>> GetDistinctCitiesAsync()
        {
            return await _dbSet.Where(t => t.IsApproved && !t.IsDeleted)
                               .Select(t => t.City)
                               .Distinct()
                               .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctSportTypesAsync()
        {
            return await _dbSet.Where(t => t.IsApproved && !t.IsDeleted)
                               .Select(t => t.SportType)
                               .Distinct()
                               .ToListAsync();
        }
    }
}
