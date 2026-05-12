using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Controllers
{
    [Authorize(Policy = "TurfManagement")]
    public class StaffController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public StaffController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var role = User.FindFirstValue(ClaimTypes.Role);
            
            IEnumerable<StaffProfile> staffProfiles;

            var allProfiles = await _unitOfWork.StaffProfiles.GetAllAsync(includeProperties: "User,User.Role,Turf");
            
            if (role == "TurfOwner")
            {
                var ownerTurfIds = (await _unitOfWork.Turfs.GetTurfsByOwnerIdAsync(userId)).Select(t => t.Id).ToList();
                staffProfiles = allProfiles.Where(s => ownerTurfIds.Contains(s.TurfId));
            }
            else // TurfManager
            {
                var turfIdStr = User.FindFirstValue("TurfId");
                if (string.IsNullOrEmpty(turfIdStr)) return Unauthorized();
                var turfId = Guid.Parse(turfIdStr);
                staffProfiles = allProfiles.Where(s => s.TurfId == turfId);
            }

            return View(staffProfiles);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

            var creatableRoleNames = turf_management_system.Models.Logic.RoleHierarchy.GetCreatableRoles(currentUserRole);
            var allRoles = await _unitOfWork.Roles.GetAllAsync();
            var availableRoles = allRoles.Where(r => creatableRoleNames.Contains(r.RoleName));

            if (currentUserRole == "TurfOwner")
            {
                var turfs = await _unitOfWork.Turfs.GetTurfsByOwnerIdAsync(userId);
                ViewBag.Turfs = new SelectList(turfs, "Id", "Name");
            }
            else // Manager
            {
                var turfIdStr = User.FindFirstValue("TurfId");
                var turfId = Guid.Parse(turfIdStr!);
                var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
                ViewBag.Turfs = new SelectList(new[] { turf }, "Id", "Name");
            }

            ViewBag.Roles = new SelectList(availableRoles, "RoleId", "RoleName");
            return View(new CreateStaffVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffVM model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

            var role = await _unitOfWork.Roles.GetByIdAsync(model.RoleId);
            if (role == null || !turf_management_system.Models.Logic.RoleHierarchy.CanCreate(currentUserRole, role.RoleName))
            {
                ModelState.AddModelError("RoleId", "You do not have permission to create this role.");
            }

            if (ModelState.IsValid)
            {
                // Validate Email Uniqueness
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already in use.");
                }
                else
                {
                    // Validate Turf Authorization
                    if (currentUserRole == "TurfOwner")
                    {
                        var turf = await _unitOfWork.Turfs.GetByIdAsync(model.TurfId);
                        if (turf == null || turf.OwnerId != userId)
                        {
                            ModelState.AddModelError("", "You do not have permission to assign staff to this turf.");
                        }
                    }
                    else // Manager
                    {
                        var turfIdStr = User.FindFirstValue("TurfId");
                        if (turfIdStr != model.TurfId.ToString())
                        {
                            ModelState.AddModelError("", "You can only assign staff to your own turf.");
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        var newUser = new User
                        {
                            FullName = model.FullName,
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                            RoleId = model.RoleId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Users.AddAsync(newUser);
                        await _unitOfWork.CompleteAsync(); // Save to get UserId

                        var staffProfile = new StaffProfile
                        {
                            UserId = newUser.UserId,
                            TurfId = model.TurfId,
                            IsActive = true,
                            HiredAt = DateTime.UtcNow
                        };

                        await _unitOfWork.StaffProfiles.AddAsync(staffProfile);
                        await _unitOfWork.CompleteAsync();

                        TempData["Success"] = "Staff member added successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            // Reload dropdowns if invalid
            var creatableRoleNames = turf_management_system.Models.Logic.RoleHierarchy.GetCreatableRoles(currentUserRole);
            var allRoles = await _unitOfWork.Roles.GetAllAsync();
            var availableRoles = allRoles.Where(r => creatableRoleNames.Contains(r.RoleName));

            ViewBag.Roles = new SelectList(availableRoles, "RoleId", "RoleName");

            if (currentUserRole == "TurfOwner")
            {
                var turfs = await _unitOfWork.Turfs.GetTurfsByOwnerIdAsync(userId);
                ViewBag.Turfs = new SelectList(turfs, "Id", "Name");
            }
            else
            {
                var turfIdStr = User.FindFirstValue("TurfId");
                var turfId = Guid.Parse(turfIdStr!);
                var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
                ViewBag.Turfs = new SelectList(new[] { turf }, "Id", "Name");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var profile = await _unitOfWork.StaffProfiles.GetByIdAsync(id);
            if (profile != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                bool isAuthorized = false;
                if (currentUserRole == "TurfOwner")
                {
                    var turf = await _unitOfWork.Turfs.GetByIdAsync(profile.TurfId);
                    if (turf != null && turf.OwnerId == currentUserId) isAuthorized = true;
                }
                else if (currentUserRole == "TurfManager")
                {
                    var turfIdStr = User.FindFirstValue("TurfId");
                    if (turfIdStr != null && turfIdStr == profile.TurfId.ToString())
                    {
                        // Strong Logic: Manager can only manage those below them in hierarchy (Level 3 < Level 4)
                        if (turf_management_system.Models.Logic.RoleHierarchy.CanManage(currentUserRole, user?.Role?.RoleName ?? ""))
                        {
                            isAuthorized = true;
                        }
                    }
                }

                if (isAuthorized)
                {
                    profile.IsActive = !profile.IsActive;
                    user.IsActive = profile.IsActive; // Sync user active state

                    _unitOfWork.StaffProfiles.Update(profile);
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.CompleteAsync();
                    TempData["Success"] = "Staff status updated.";
                }
                else
                {
                    TempData["Error"] = "Unauthorized to perform this action.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
