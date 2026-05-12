using turf_management_system.Data;
using turf_management_system.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace turf_management_system.BackgroundJobs
{
    /// <summary>
    /// Runs every 60 seconds to release expired slot locks and expire timed-out bookings.
    /// </summary>
    public class SlotLockCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SlotLockCleanupService> _logger;

        public SlotLockCleanupService(IServiceScopeFactory scopeFactory, ILogger<SlotLockCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SlotLockCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredLocksAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SlotLockCleanupService.");
                }

                // Run every 60 seconds
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        private async Task CleanupExpiredLocksAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            // Release expired locks
            var expiredLocks = await context.SlotLocks
                .Where(l => !l.IsReleased && l.LockedUntil <= now)
                .ToListAsync();

            if (!expiredLocks.Any()) return;

            foreach (var lock_ in expiredLocks)
            {
                lock_.IsReleased = true;
            }

            // Find bookings still in PendingPayment state whose locks have expired
            var bookingIds = expiredLocks
                .Where(l => l.BookingId.HasValue)
                .Select(l => l.BookingId!.Value)
                .ToList();

            if (bookingIds.Any())
            {
                var expiredBookings = await context.Bookings
                    .Where(b => bookingIds.Contains(b.Id) && b.Status == BookingStatus.PendingPayment)
                    .ToListAsync();

                foreach (var booking in expiredBookings)
                {
                    booking.Status = BookingStatus.Expired;
                    booking.UpdatedAt = now;
                }

                _logger.LogInformation(
                    "Cleanup: Released {LockCount} expired locks, expired {BookingCount} bookings.",
                    expiredLocks.Count, expiredBookings.Count);
            }

            await context.SaveChangesAsync();
        }
    }
}
