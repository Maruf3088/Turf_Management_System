using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.DTOs.Booking;
using turf_management_system.Models.Domain;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers.Api
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        [Authorize(Roles = "NormalUser")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.CreateBookingAsync(dto, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "NormalUser")]
        public async Task<IActionResult> GetMyBookings([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] BookingStatus? status = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetMyBookingsAsync(userId, pageNumber, pageSize, status);
            return Ok(result);
        }

        [HttpGet("turf/{turfId}")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> GetTurfBookings(Guid turfId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] BookingStatus? status = null)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetTurfBookingsAsync(turfId, ownerId, pageNumber, pageSize, status);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(Guid id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var result = await _bookingService.GetBookingByIdAsync(id, userId, role);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPatch("{id}/confirm")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> ConfirmBooking(Guid id)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.ConfirmBookingAsync(id, ownerId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "TurfOwner")]
        public async Task<IActionResult> RejectBooking(Guid id, [FromBody] string reason)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.RejectBookingAsync(id, ownerId, reason);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var result = await _bookingService.CancelBookingAsync(id, userId, role);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
