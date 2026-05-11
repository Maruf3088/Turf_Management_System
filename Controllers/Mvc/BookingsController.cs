using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers.Mvc
{
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Confirmation(Guid id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var result = await _bookingService.GetBookingByIdAsync(id, userId, role);
            
            if (!result.Success) return NotFound();
            return View(result.Data);
        }
    }
}
