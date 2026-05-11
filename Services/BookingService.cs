using turf_management_system.DTOs.Booking;
using turf_management_system.DTOs.Common;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(CreateBookingDto dto, int userId)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(dto.TurfId);
            if (turf == null) return ApiResponse<BookingResponseDto>.FailureResponse("Turf not found.");

            var slot = await _unitOfWork.TurfSlots.GetByIdAsync(dto.SlotId);
            if (slot == null || slot.TurfId != dto.TurfId) return ApiResponse<BookingResponseDto>.FailureResponse("Invalid slot.");

            if (dto.BookingDate < DateOnly.FromDateTime(DateTime.UtcNow))
                return ApiResponse<BookingResponseDto>.FailureResponse("Booking date cannot be in the past.");

            bool alreadyBooked = await _unitOfWork.Bookings.IsSlotAlreadyBookedAsync(dto.SlotId, dto.BookingDate);
            if (alreadyBooked) return ApiResponse<BookingResponseDto>.FailureResponse("Slot is already booked for this date.");

            var totalHours = (decimal)(slot.EndTime - slot.StartTime).TotalHours;
            var totalAmount = totalHours * turf.PricePerHour;

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                TurfId = dto.TurfId,
                UserId = userId,
                SlotId = dto.SlotId,
                BookingDate = dto.BookingDate,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                TotalHours = totalHours,
                TotalAmount = totalAmount,
                Status = BookingStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid,
                SpecialRequest = dto.SpecialRequest,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<BookingResponseDto>.SuccessResponse(MapToResponseDto(booking), "Booking created successfully. Pending owner confirmation.");
        }

        public async Task<ApiResponse<bool>> ConfirmBookingAsync(Guid bookingId, int ownerId)
        {
            var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return ApiResponse<bool>.FailureResponse("Booking not found.");

            if (booking.Turf.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            if (booking.Status != BookingStatus.Pending)
                return ApiResponse<bool>.FailureResponse("Only pending bookings can be confirmed.");

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Booking confirmed successfully.");
        }

        public async Task<ApiResponse<bool>> RejectBookingAsync(Guid bookingId, int ownerId, string reason)
        {
            var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return ApiResponse<bool>.FailureResponse("Booking not found.");

            if (booking.Turf.OwnerId != ownerId) return ApiResponse<bool>.FailureResponse("Unauthorized.");

            booking.Status = BookingStatus.Rejected;
            booking.CancellationReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Booking rejected.");
        }

        public async Task<ApiResponse<bool>> CancelBookingAsync(Guid bookingId, int requesterId, string requesterRole)
        {
            var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return ApiResponse<bool>.FailureResponse("Booking not found.");

            if (requesterRole != "Admin" && booking.UserId != requesterId)
                return ApiResponse<bool>.FailureResponse("Unauthorized.");

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                return ApiResponse<bool>.FailureResponse("Cannot cancel this booking.");

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.CompleteAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Booking cancelled successfully.");
        }

        public async Task<ApiResponse<PagedResultDto<BookingResponseDto>>> GetMyBookingsAsync(int userId, int pageNumber, int pageSize, BookingStatus? status)
        {
            var (items, totalCount) = await _unitOfWork.Bookings.GetPagedAsync(pageNumber, pageSize, userId, null, status);

            var result = new PagedResultDto<BookingResponseDto>
            {
                Items = items.Select(MapToResponseDto),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<BookingResponseDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<PagedResultDto<BookingResponseDto>>> GetTurfBookingsAsync(Guid turfId, int ownerId, int pageNumber, int pageSize, BookingStatus? status)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null || turf.OwnerId != ownerId) return ApiResponse<PagedResultDto<BookingResponseDto>>.FailureResponse("Unauthorized.");

            var (items, totalCount) = await _unitOfWork.Bookings.GetPagedAsync(pageNumber, pageSize, null, turfId, status);

            var result = new PagedResultDto<BookingResponseDto>
            {
                Items = items.Select(MapToResponseDto),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<BookingResponseDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<BookingResponseDto>> GetBookingByIdAsync(Guid bookingId, int requesterId, string requesterRole)
        {
            var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return ApiResponse<BookingResponseDto>.FailureResponse("Booking not found.");

            if (requesterRole != "Admin" && booking.UserId != requesterId && booking.Turf.OwnerId != requesterId)
                return ApiResponse<BookingResponseDto>.FailureResponse("Unauthorized.");

            return ApiResponse<BookingResponseDto>.SuccessResponse(MapToResponseDto(booking));
        }

        public async Task<ApiResponse<IEnumerable<SlotAvailabilityDto>>> GetAvailableSlotsAsync(Guid turfId, DateOnly date)
        {
            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(turfId);
            if (turf == null) return ApiResponse<IEnumerable<SlotAvailabilityDto>>.FailureResponse("Turf not found.");

            var dayOfWeek = (int)date.DayOfWeek;
            var daySlots = turf.Slots.Where(s => s.DayOfWeek == null || s.DayOfWeek == dayOfWeek);

            var existingBookings = await _unitOfWork.Bookings.GetBookingsForTurfOnDateAsync(turfId, date);
            var bookedSlotIds = existingBookings.Select(b => b.SlotId).ToHashSet();

            var result = daySlots.Select(s => new SlotAvailabilityDto
            {
                SlotId = s.Id,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsAvailable = s.IsAvailable,
                IsAlreadyBooked = bookedSlotIds.Contains(s.Id)
            });

            return ApiResponse<IEnumerable<SlotAvailabilityDto>>.SuccessResponse(result);
        }

        private BookingResponseDto MapToResponseDto(Booking booking)
        {
            return new BookingResponseDto
            {
                Id = booking.Id,
                TurfId = booking.TurfId,
                TurfName = booking.Turf?.Name ?? "Unknown",
                TurfCity = booking.Turf?.City ?? "Unknown",
                MainImageUrl = booking.Turf?.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "",
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                BookingDate = booking.BookingDate,
                TotalHours = booking.TotalHours,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                UserName = booking.User?.FullName ?? "Unknown",
                OwnerName = booking.Turf?.Owner?.FullName ?? "Unknown",
                SpecialRequest = booking.SpecialRequest,
                CancellationReason = booking.CancellationReason,
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
