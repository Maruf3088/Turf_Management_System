using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        Task<(IEnumerable<Booking> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, int? userId, Guid? turfId, BookingStatus? status, int? ownerId = null);
        Task<bool> IsSlotAlreadyBookedAsync(Guid slotId, DateOnly date);
        Task<IEnumerable<Booking>> GetBookingsForTurfOnDateAsync(Guid turfId, DateOnly date);
        Task<Booking?> GetBookingWithDetailsAsync(Guid bookingId);
    }
}
