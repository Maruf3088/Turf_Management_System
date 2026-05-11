using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ITurfService _turfService;

        public HomeController(ITurfService turfService)
        {
            _turfService = turfService;
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

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
