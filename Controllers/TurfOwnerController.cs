using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;
using turf_management_system.DTOs.Turf;

namespace turf_management_system.Controllers
{
    [Authorize(Roles = "TurfOwner")]
    public class TurfOwnerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITurfService _turfService;

        public TurfOwnerController(IUnitOfWork unitOfWork, ITurfService turfService)
        {
            _unitOfWork = unitOfWork;
            _turfService = turfService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fullName = User.FindFirstValue(ClaimTypes.Name) ?? "Turf Owner";
            
            // In a real app, we would check the database for the user's active status
            // For now, we use placeholders as requested
            var viewModel = new TurfOwnerDashboardVM
            {
                FullName = fullName,
                MyTurfs = 0,
                TodaysBookings = 0,
                TotalBookings = 0,
                IsActive = true
            };

            return View(viewModel);
        }

        public async Task<IActionResult> MyTurfs()
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.GetMyTurfsAsync(ownerId);
            return View(result.Data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateTurfDto());
        }

        // Updated for AJAX flow - redirects to MyTurfs for non-AJAX or fallback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTurfDto model)
        {
            if (ModelState.IsValid)
            {
                var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _turfService.CreateTurfAsync(model, ownerId);
                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    return RedirectToAction(nameof(MyTurfs));
                }
                ModelState.AddModelError("", result.Message);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Slots(Guid id)
        {
            var result = await _turfService.GetTurfByIdAsync(id);
            if (!result.Success) return NotFound();
            
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (result.Data!.OwnerId != ownerId) return Forbid();

            return View(result.Data);
        }

        [HttpGet]
        public IActionResult Bookings()
        {
            return View();
        }
    }
}
