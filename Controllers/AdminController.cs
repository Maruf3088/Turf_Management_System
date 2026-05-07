using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using turf_management_system.Models.Domain;
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

        public async Task<IActionResult> Users(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int? roleId = null)
        {
            ViewBag.Roles = await _unitOfWork.Roles.GetAllAsync();

            var pagedUsers = await _unitOfWork.Users.GetPagedAsync(
                pageNumber, 
                pageSize, 
                u => (string.IsNullOrEmpty(searchTerm) || u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm))
                     && (roleId == null || u.RoleId == roleId),
                query => query.OrderByDescending(u => u.CreatedAt),
                "Role"
            );

            var viewModel = new UserListVM
            {
                PagedUsers = pagedUsers,
                SearchTerm = searchTerm,
                RoleId = roleId
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

        #region Role Management

        public async Task<IActionResult> Roles(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var pagedRoles = await _unitOfWork.Roles.GetPagedAsync(
                pageNumber,
                pageSize,
                r => string.IsNullOrEmpty(searchTerm) || r.RoleName.Contains(searchTerm),
                query => query.OrderBy(r => r.RoleName)
            );

            var viewModel = new RoleListVM
            {
                PagedRoles = pagedRoles,
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View(new RoleVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(RoleVM model)
        {
            if (ModelState.IsValid)
            {
                var existingRole = await _unitOfWork.Roles.FindAsync(r => r.RoleName == model.RoleName);
                if (existingRole != null)
                {
                    ModelState.AddModelError("RoleName", "Role already exists.");
                    return View(model);
                }

                var role = new Role
                {
                    RoleName = model.RoleName,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Roles.AddAsync(role);
                await _unitOfWork.CompleteAsync();

                TempData["Success"] = "Role created successfully.";
                return RedirectToAction(nameof(Roles));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(int id)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null) return NotFound();

            var viewModel = new RoleVM
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RoleVM model)
        {
            if (ModelState.IsValid)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(model.RoleId);
                if (role == null) return NotFound();

                var existingRole = await _unitOfWork.Roles.FindAsync(r => r.RoleName == model.RoleName && r.RoleId != model.RoleId);
                if (existingRole != null)
                {
                    ModelState.AddModelError("RoleName", "Another role with this name already exists.");
                    return View(model);
                }

                role.RoleName = model.RoleName;
                _unitOfWork.Roles.Update(role);
                await _unitOfWork.CompleteAsync();

                TempData["Success"] = "Role updated successfully.";
                return RedirectToAction(nameof(Roles));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role != null)
            {
                // Check if any users are assigned to this role
                var usersWithRole = await _unitOfWork.Users.GetCountAsync(u => u.RoleId == roleId);
                if (usersWithRole > 0)
                {
                    TempData["Error"] = "Cannot delete role as it is assigned to existing users.";
                    return RedirectToAction(nameof(Roles));
                }

                _unitOfWork.Roles.Delete(role);
                await _unitOfWork.CompleteAsync();
                TempData["Success"] = "Role deleted successfully.";
            }
            return RedirectToAction(nameof(Roles));
        }

        #endregion
    }
}
