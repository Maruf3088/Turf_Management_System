using turf_management_system.DTOs.Common;
using turf_management_system.DTOs.Booking;
using turf_management_system.Models.Domain;

namespace turf_management_system.Services.Interfaces
{
    public interface IBookingService
    {
        Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(CreateBookingDto dto, int userId);
        Task<ApiResponse<bool>> ConfirmBookingAsync(Guid bookingId, int ownerId);
        Task<ApiResponse<bool>> RejectBookingAsync(Guid bookingId, int ownerId, string reason);
        Task<ApiResponse<bool>> CancelBookingAsync(Guid bookingId, int requesterId, string requesterRole);
        Task<ApiResponse<PagedResultDto<BookingResponseDto>>> GetMyBookingsAsync(int userId, int pageNumber, int pageSize, BookingStatus? status);
        Task<ApiResponse<PagedResultDto<BookingResponseDto>>> GetTurfBookingsAsync(Guid turfId, int ownerId, int pageNumber, int pageSize, BookingStatus? status);
        Task<ApiResponse<BookingResponseDto>> GetBookingByIdAsync(Guid bookingId, int requesterId, string requesterRole);
        Task<ApiResponse<IEnumerable<SlotAvailabilityDto>>> GetAvailableSlotsAsync(Guid turfId, DateOnly date);
    }
}
