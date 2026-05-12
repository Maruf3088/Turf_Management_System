using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(AppDbContext context) : base(context) { }

        public async Task<(IEnumerable<Booking> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, int? userId, Guid? turfId, BookingStatus? status, int? ownerId = null)
        {
            var query = _context.Bookings
                .Include(b => b.Turf)
                .Include(b => b.Slot)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .AsQueryable();

            if (userId.HasValue) query = query.Where(b => b.UserId == userId.Value);
            if (turfId.HasValue) query = query.Where(b => b.TurfId == turfId.Value);
            if (status.HasValue) query = query.Where(b => b.Status == status.Value);
            if (ownerId.HasValue) query = query.Where(b => b.Turf.OwnerId == ownerId.Value);

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Checks if a slot time range on a given date is already booked (overlapping active bookings).
        /// Uses time overlap logic: existing.Start < requested.End AND existing.End > requested.Start
        /// </summary>
        public async Task<bool> IsSlotAlreadyBookedAsync(Guid slotId, DateOnly date)
        {
            return await _context.Bookings.AnyAsync(b =>
                b.SlotId == slotId &&
                b.BookingDate == date &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Expired &&
                b.Status != BookingStatus.Refunded);
        }

        public async Task<IEnumerable<Booking>> GetBookingsForTurfOnDateAsync(Guid turfId, DateOnly date)
        {
            return await _context.Bookings
                .Include(b => b.Slot)
                .Where(b => b.TurfId == turfId
                         && b.BookingDate == date
                         && b.Status != BookingStatus.Cancelled
                         && b.Status != BookingStatus.Expired
                         && b.Status != BookingStatus.Refunded)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingWithDetailsAsync(Guid bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Turf).ThenInclude(t => t.Owner)
                .Include(b => b.Turf).ThenInclude(t => t.Images)
                .Include(b => b.Slot)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }
    }
}
