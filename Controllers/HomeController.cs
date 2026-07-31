using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turf_management_system.Services.Interfaces;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Models.ViewModels;

namespace turf_management_system.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ITurfService _turfService;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ITurfService turfService, IUnitOfWork unitOfWork)
        {
            _turfService = turfService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _turfService.GetAllTurfsPagedAsync(1, 6, null, null, null, true);
            return View(result.Data?.Items);
        }

        public async Task<IActionResult> AllTurfs(int pageNumber = 1, int pageSize = 12, string? search = null, string? city = null, string? sportType = null)
        {
            var result = await _turfService.GetAllTurfsPagedAsync(pageNumber, pageSize, search, city, sportType, true);
            return View(result.Data);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _turfService.GetTurfByIdAsync(id);
            if (!result.Success) return NotFound();
            return View(result.Data);
        }

        // AJAX Metadata
        [HttpGet("api/home/cities")]
        public async Task<IActionResult> GetCities() => Ok(await _turfService.GetCitiesAsync());

        [HttpGet("api/home/sport-types")]
        public async Task<IActionResult> GetSportTypes() => Ok(await _turfService.GetSportTypesAsync());

        public async Task<IActionResult> OwnerProfile(int id)
        {
            var owner = await _unitOfWork.TurfOwners.FindAsync(o => o.UserId == id, includeProperties: "User");
            var user = owner?.User ?? await _unitOfWork.Users.GetByIdAsync(id);

            if (user == null) return NotFound();

            // Load all active, approved turfs for this owner
            var turfs = await _unitOfWork.Turfs.GetTurfsByOwnerIdAsync(id);
            var activeTurfs = turfs.Where(t => t.IsApproved && !t.IsDeleted).ToList();

            // Map turfs to response DTOs
            var turfDtos = new List<turf_management_system.DTOs.Turf.TurfResponseDto>();
            foreach (var turf in activeTurfs)
            {
                var res = await _turfService.GetTurfByIdAsync(turf.Id);
                if (res.Success && res.Data != null)
                    turfDtos.Add(res.Data);
            }

            var vm = new OwnerProfileVM
            {
                OwnerId         = id,
                OwnerName       = user.FullName,
                BusinessName    = string.IsNullOrWhiteSpace(owner?.BusinessName) ? user.FullName + " Arena" : owner.BusinessName,
                BusinessAddress = owner?.BusinessAddress,
                ContactNumber   = owner?.ContactNumber ?? user.PhoneNumber,
                IsActive        = user.IsActive,
                MemberSince     = owner?.CreatedAt ?? user.CreatedAt,
                Turfs           = turfDtos
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
