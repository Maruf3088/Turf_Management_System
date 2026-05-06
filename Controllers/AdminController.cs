using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _unitOfWork.Users.GetCountAsync();
            ViewBag.ActiveUsers = await _unitOfWork.Users.GetCountAsync(u => u.IsActive);
            ViewBag.TotalRoles = (await _unitOfWork.Roles.GetAllAsync()).Count();

            return View();
        }

        public async Task<IActionResult> Users(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var pagedUsers = await _unitOfWork.Users.GetPagedAsync(
                pageNumber, 
                pageSize, 
                u => string.IsNullOrEmpty(searchTerm) || u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm),
                query => query.OrderByDescending(u => u.CreatedAt),
                "Role"
            );

            var viewModel = new UserListVM
            {
                PagedUsers = pagedUsers,
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();
                
                TempData["Success"] = $"User {user.FullName} status updated.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
