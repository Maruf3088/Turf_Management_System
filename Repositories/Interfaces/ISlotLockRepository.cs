using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface ISlotLockRepository : IGenericRepository<SlotLock>
    {
        /// <summary>Check if a time range is already locked by another user on a given date.</summary>
        Task<SlotLock?> GetActiveLockAsync(Guid turfId, DateOnly date, TimeSpan startTime, TimeSpan endTime);

        /// <summary>Check if a specific booking has an active lock.</summary>
        Task<SlotLock?> GetLockByBookingIdAsync(Guid bookingId);

        /// <summary>Get all expired, unreleased locks.</summary>
        Task<IEnumerable<SlotLock>> GetExpiredLocksAsync();

        /// <summary>Release all expired locks (called by background job).</summary>
        Task ReleaseExpiredLocksAsync();
    }
}
