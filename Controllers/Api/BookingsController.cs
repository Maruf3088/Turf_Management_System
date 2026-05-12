using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers.Api
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IUnitOfWork _unitOfWork;

        public BookingsController(IBookingService bookingService, IUnitOfWork unitOfWork)
        {
            _bookingService = bookingService;
            _unitOfWork = unitOfWork;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>GET api/bookings/slots/{turfId}?date=YYYY-MM-DD — real-time slot availability</summary>
        [HttpGet("slots/{turfId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSlots(Guid turfId, [FromQuery] string date)
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
                return BadRequest(new { success = false, message = "Invalid date format. Use YYYY-MM-DD." });

            var slots = await _bookingService.GetAvailableSlotsAsync(turfId, parsedDate);
            return Ok(new { success = true, data = slots });
        }

        /// <summary>POST api/bookings/lock — Lock slot and create PendingPayment booking</summary>
        [HttpPost("lock")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> LockSlot([FromBody] LockSlotRequest request)
        {
            var userId = GetUserId();
            var (success, message, bookingId) = await _bookingService.LockSlotAndCreateBookingAsync(
                request.TurfId, request.SlotId, request.BookingDate, userId, request.SpecialRequest);

            if (!success) return BadRequest(new { success = false, message });
            return Ok(new { success = true, message, bookingId });
        }

        /// <summary>GET api/bookings/my — Customer's bookings</summary>
        [HttpGet("my")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyBookings(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] BookingStatus? status = null)
        {
            var userId = GetUserId();
            var (items, totalCount) = await _bookingService.GetMyBookingsAsync(userId, pageNumber, pageSize, status);
            return Ok(new { success = true, data = items, totalCount });
        }

        /// <summary>GET api/bookings/{id} — Get booking by ID (ownership enforced)</summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(Guid id)
        {
            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var booking = await _bookingService.GetBookingWithDetailsAsync(id, userId, role);
            if (booking == null) return NotFound(new { success = false, message = "Booking not found or unauthorized." });
            return Ok(new { success = true, data = booking });
        }

        /// <summary>POST api/bookings/{id}/cancel — Cancel booking</summary>
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var (success, message) = await _bookingService.CancelBookingAsync(id, userId, role);
            if (!success) return BadRequest(new { success = false, message });
            return Ok(new { success = true, message });
        }

        /// <summary>POST api/bookings/{id}/submit-payment — Submit payment for a booking</summary>
        [HttpPost("{id}/submit-payment")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> SubmitPayment(Guid id, [FromBody] SubmitPaymentRequest request)
        {
            var userId = GetUserId();
            var (success, message) = await _bookingService.SubmitPaymentAsync(
                id, userId, request.TransactionId, request.PaymentMethod, request.Amount, request.PaymentType);

            if (!success) return BadRequest(new { success = false, message });
            return Ok(new { success = true, message });
        }

        /// <summary>POST api/bookings/payments/{paymentId}/verify — Verify payment (owner)</summary>
        [HttpPost("payments/{paymentId}/verify")]
        [Authorize(Roles = "TurfOwner,TurfManager")]
        public async Task<IActionResult> VerifyPayment(Guid paymentId)
        {
            var userId = GetUserId();
            var (success, message) = await _bookingService.VerifyPaymentAsync(paymentId, userId);
            if (!success) return BadRequest(new { success = false, message });
            return Ok(new { success = true, message });
        }

        /// <summary>POST api/bookings/payments/{paymentId}/reject — Reject payment (owner)</summary>
        [HttpPost("payments/{paymentId}/reject")]
        [Authorize(Roles = "TurfOwner,TurfManager")]
        public async Task<IActionResult> RejectPayment(Guid paymentId, [FromBody] string reason)
        {
            var userId = GetUserId();
            var (success, message) = await _bookingService.RejectPaymentAsync(paymentId, userId, reason);
            if (!success) return BadRequest(new { success = false, message });
            return Ok(new { success = true, message });
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────
    public record LockSlotRequest(Guid TurfId, Guid SlotId, DateOnly BookingDate, string? SpecialRequest);
    public record SubmitPaymentRequest(string TransactionId, turf_management_system.Models.Domain.PaymentMethod PaymentMethod, decimal Amount, turf_management_system.Models.Domain.PaymentType PaymentType);
}
