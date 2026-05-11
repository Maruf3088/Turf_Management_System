using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Booking> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, int? userId, Guid? turfId, BookingStatus? status)
        {
            var query = _context.Bookings
                .Include(b => b.Turf)
                .Include(b => b.Slot)
                .Include(b => b.User)
                .AsQueryable();

            if (userId.HasValue) query = query.Where(b => b.UserId == userId.Value);
            if (turfId.HasValue) query = query.Where(b => b.TurfId == turfId.Value);
            if (status.HasValue) query = query.Where(b => b.Status == status.Value);

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> IsSlotAlreadyBookedAsync(Guid slotId, DateOnly date)
        {
            return await _context.Bookings.AnyAsync(b => 
                b.SlotId == slotId && 
                b.BookingDate == date && 
                b.Status != BookingStatus.Cancelled && 
                b.Status != BookingStatus.Rejected);
        }

        public async Task<IEnumerable<Booking>> GetBookingsForTurfOnDateAsync(Guid turfId, DateOnly date)
        {
            return await _context.Bookings
                .Where(b => b.TurfId == turfId && b.BookingDate == date && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingWithDetailsAsync(Guid bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Turf)
                .Include(b => b.Slot)
                .Include(b => b.User)
                .Include(b => b.Turf.Owner)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }
    }
}
