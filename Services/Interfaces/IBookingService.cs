using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;

namespace turf_management_system.Services.Interfaces
{
    public interface IBookingService
    {
        /// <summary>Step 1: Lock a slot and create a PendingPayment booking.</summary>
        Task<(bool Success, string Message, Guid? BookingId)> LockSlotAndCreateBookingAsync(Guid turfId, Guid slotId, DateOnly bookingDate, int userId, string? specialRequest);

        /// <summary>Step 2: Submit payment transaction ID for verification.</summary>
        Task<(bool Success, string Message)> SubmitPaymentAsync(Guid bookingId, int userId, string transactionId, PaymentMethod paymentMethod, decimal amount, PaymentType paymentType, bool autoVerify = false);


        /// <summary>Step 3 (Owner): Verify a submitted payment and confirm the booking.</summary>
        Task<(bool Success, string Message)> VerifyPaymentAsync(Guid paymentId, int verifierUserId);

        /// <summary>Reject a payment submission with a reason.</summary>
        Task<(bool Success, string Message)> RejectPaymentAsync(Guid paymentId, int verifierUserId, string reason);

        /// <summary>Cancel a booking, applying refund rules if payment was made.</summary>
        Task<(bool Success, string Message)> CancelBookingAsync(Guid bookingId, int requesterId, string requesterRole);

        /// <summary>Get available slots for a turf on a specific date (respects config + existing bookings + active locks).</summary>
        Task<IEnumerable<SlotAvailabilityVM>> GetAvailableSlotsAsync(Guid turfId, DateOnly date, int? currentUserId = null);


        /// <summary>Get booking details (ownership-checked).</summary>
        Task<Booking?> GetBookingWithDetailsAsync(Guid bookingId, int requesterId, string requesterRole);

        /// <summary>Get paginated bookings for a customer.</summary>
        Task<(IEnumerable<Booking> Items, int TotalCount)> GetMyBookingsAsync(int userId, int pageNumber, int pageSize, BookingStatus? status);

        /// <summary>Get paginated bookings for a turf (owner-scoped).</summary>
        Task<(IEnumerable<Booking> Items, int TotalCount)> GetTurfBookingsAsync(Guid turfId, int ownerId, int pageNumber, int pageSize);

        /// <summary>Get all bookings across all turfs owned by an owner.</summary>
        Task<(IEnumerable<Booking> Items, int TotalCount)> GetOwnerAllBookingsAsync(int ownerId, int pageNumber, int pageSize, BookingStatus? status);

        /// <summary>Get pending payment submissions for a turf owner to verify.</summary>
        Task<IEnumerable<Payment>> GetPendingPaymentsForOwnerAsync(int ownerId);
    }
}
