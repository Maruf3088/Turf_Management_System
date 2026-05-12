using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class SlotLockRepository : GenericRepository<SlotLock>, ISlotLockRepository
    {
        public SlotLockRepository(AppDbContext context) : base(context) { }

        public async Task<SlotLock?> GetActiveLockAsync(Guid turfId, DateOnly date, TimeSpan startTime, TimeSpan endTime)
        {
            // Check for any active lock that overlaps with the requested time range
            return await _dbSet
                .Where(l => l.TurfId == turfId
                         && l.BookingDate == date
                         && !l.IsReleased
                         && l.LockedUntil > DateTime.UtcNow
                         && l.StartTime < endTime    // Overlap condition: existing start < requested end
                         && l.EndTime > startTime)   // Overlap condition: existing end > requested start
                .FirstOrDefaultAsync();
        }

        public async Task<SlotLock?> GetLockByBookingIdAsync(Guid bookingId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(l => l.BookingId == bookingId && !l.IsReleased);
        }

        public async Task<IEnumerable<SlotLock>> GetExpiredLocksAsync()
        {
            return await _dbSet
                .Where(l => !l.IsReleased && l.LockedUntil <= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task ReleaseExpiredLocksAsync()
        {
            var expiredLocks = await GetExpiredLocksAsync();
            foreach (var lock_ in expiredLocks)
            {
                lock_.IsReleased = true;
            }
            // SaveChanges is called by UnitOfWork.CompleteAsync()
        }
    }
}
