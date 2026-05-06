using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Controllers
{
    [Authorize(Roles = "TurfOwner")]
    public class TurfOwnerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public TurfOwnerController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
    }
}
