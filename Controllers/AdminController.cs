using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers
{
    [Authorize(Policy = "PlatformAdmins")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITurfService _turfService;

        public AdminController(IUnitOfWork unitOfWork, ITurfService turfService)
        {
            _unitOfWork = unitOfWork;
            _turfService = turfService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var users = await _unitOfWork.Users.GetAllAsync(includeProperties: "Role");
            
            ViewBag.TotalAdmins = users.Count(u => new[] { "SuperAdmin", "Admin", "SupportAdmin", "FinanceAdmin", "OperationsAdmin" }.Contains(u.Role?.RoleName));
            ViewBag.TotalOwners = users.Count(u => u.Role?.RoleName == "TurfOwner");
            ViewBag.TotalStaff = users.Count(u => new[] { "TurfManager", "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard" }.Contains(u.Role?.RoleName));
            ViewBag.TotalCustomers = users.Count(u => u.Role?.RoleName == "User");
            ViewBag.ActiveUsers = users.Count(u => u.IsActive);
            
            var turfs = await _unitOfWork.Turfs.GetAllAsync();
            ViewBag.TotalTurfs = turfs.Count();

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.UserId == userId, includeProperties: "Role");
            if (user != null)
            {
                var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
                
                if (turf_management_system.Models.Logic.RoleHierarchy.CanManage(currentUserRole, user.Role?.RoleName ?? ""))
                {
                    user.IsActive = !user.IsActive;
                    user.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.CompleteAsync();
                    
                    TempData["Success"] = $"User {user.FullName} status updated.";
                }
                else
                {
                    TempData["Error"] = "You do not have permission to manage this user.";
                }
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.UserId == userId, includeProperties: "Role");
            if (user == null) return NotFound();

            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (!turf_management_system.Models.Logic.RoleHierarchy.CanManage(currentUserRole, user.Role?.RoleName ?? ""))
            {
                TempData["Error"] = "You do not have permission to delete this user.";
                return RedirectToAction(nameof(Users));
            }

            // Check for active bookings before deletion
            var activeBookings = await _unitOfWork.Bookings.GetCountAsync(b => b.UserId == userId && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.PendingPayment));
            if (activeBookings > 0)
            {
                TempData["Error"] = "Cannot delete user with active/pending bookings. Cancel them first.";
                return RedirectToAction(nameof(Users));
            }

            // Real-life DB integrity check
            bool hasBookings = await _unitOfWork.Bookings.GetCountAsync(b => b.UserId == userId) > 0;
            bool hasPayments = await _unitOfWork.Payments.GetCountAsync(p => p.Booking.UserId == userId) > 0;
            bool hasTurfs = await _unitOfWork.Turfs.GetCountAsync(t => t.OwnerId == userId) > 0;
            bool hasStaff = await _unitOfWork.StaffProfiles.GetCountAsync(s => s.UserId == userId) > 0;
            bool hasTurfOwner = await _unitOfWork.TurfOwners.GetCountAsync(o => o.UserId == userId) > 0;

            if (hasBookings || hasPayments || hasTurfs || hasStaff || hasTurfOwner)
            {
                // Soft delete by deactivating the account to prevent DB foreign key violations and preserve history
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);

                // If Turf Owner, also deactivate all their turfs
                if (user.Role?.RoleName == "TurfOwner")
                {
                    var turfs = await _unitOfWork.Turfs.GetAllAsync();
                    var ownerTurfs = turfs.Where(t => t.OwnerId == userId).ToList();
                    foreach (var turf in ownerTurfs)
                    {
                        turf.IsActive = false;
                        turf.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Turfs.Update(turf);
                    }
                }

                await _unitOfWork.CompleteAsync();
                TempData["Success"] = $"User '{user.FullName}' has associated transactions or records. To maintain database integrity, the account has been deactivated instead of permanently deleted.";
                return RedirectToAction(nameof(Users));
            }

            // Safe to permanently delete
            var owner = await _unitOfWork.TurfOwners.FindAsync(o => o.UserId == userId);
            if (owner != null)
            {
                _unitOfWork.TurfOwners.Delete(owner);
            }
            _unitOfWork.Users.Delete(user);
            await _unitOfWork.CompleteAsync();
            
            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.UserId == id, includeProperties: "Role");
            if (user == null) return NotFound();

            if (user.Role == null && user.RoleId > 0)
            {
                user.Role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId) ?? new Role { RoleName = "User" };
            }

            var roleName = user.Role?.RoleName ?? "";

            // Load associated data based on Role
            if (roleName.Equals("TurfOwner", StringComparison.OrdinalIgnoreCase))
            {
                var ownerProfile = await _unitOfWork.TurfOwners.FindAsync(o => o.UserId == id);
                ViewBag.OwnerProfile = ownerProfile;
                
                var turfs = await _unitOfWork.Turfs.GetAllAsync();
                var ownerTurfs = turfs.Where(t => t.OwnerId == id).ToList();
                ViewBag.Turfs = ownerTurfs;
            }
            else if (roleName.Equals("User", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                var bookings = await _unitOfWork.Bookings.GetAllAsync(includeProperties: "Turf");
                var userBookings = bookings.Where(b => b.UserId == id).OrderByDescending(b => b.CreatedAt).ToList();
                ViewBag.Bookings = userBookings;
            }
            else if (new[] { "TurfManager", "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard" }.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
            {
                var staffProfile = await _unitOfWork.StaffProfiles.FindAsync(s => s.UserId == id, includeProperties: "Turf");
                ViewBag.StaffProfile = staffProfile;
            }

            return View(user);
        }

        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Admins(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var adminRoles = new[] { "Admin", "SupportAdmin", "FinanceAdmin", "OperationsAdmin" };
            
            var pagedAdmins = await _unitOfWork.Users.GetPagedAsync(
                pageNumber, 
                pageSize, 
                u => adminRoles.Contains(u.Role.RoleName) && (string.IsNullOrEmpty(searchTerm) || u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm)),
                query => query.OrderByDescending(u => u.CreatedAt),
                "Role"
            );

            var viewModel = new UserListVM
            {
                PagedUsers = pagedAdmins,
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var creatableRoleNames = turf_management_system.Models.Logic.RoleHierarchy.GetCreatableRoles(currentUserRole);
            
            var allRoles = await _unitOfWork.Roles.GetAllAsync();
            var filteredRoles = allRoles.Where(r => creatableRoleNames.Contains(r.RoleName));
            
            ViewBag.Roles = new SelectList(filteredRoles, "RoleId", "RoleName");
            return View(new CreateStaffVM());
        }

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateStaffVM model)
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var role = await _unitOfWork.Roles.GetByIdAsync(model.RoleId);
            
            if (role == null || !turf_management_system.Models.Logic.RoleHierarchy.CanCreate(currentUserRole, role.RoleName))
            {
                ModelState.AddModelError("RoleId", "You do not have permission to create this role.");
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already in use.");
                }
                else
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
                    await _unitOfWork.CompleteAsync();

                    if (role.RoleName == "TurfOwner")
                    {
                        var turfOwner = new TurfOwner
                        {
                            UserId = newUser.UserId,
                            BusinessName = "New Turf Business",
                            VerificationStatus = turf_management_system.Models.Enums.VerificationStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.TurfOwners.AddAsync(turfOwner);
                        await _unitOfWork.CompleteAsync();
                    }

                    TempData["Success"] = $"{role.RoleName} created successfully.";
                    return RedirectToAction(nameof(Admins));
                }
            }

            var allRoles = await _unitOfWork.Roles.GetAllAsync();
            var adminRoles = allRoles.Where(r => new[] { "Admin", "SupportAdmin", "FinanceAdmin", "OperationsAdmin" }.Contains(r.RoleName));
            ViewBag.Roles = new SelectList(adminRoles, "RoleId", "RoleName");
            return View(model);
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

        #region Turf Management

        public async Task<IActionResult> Turfs(int pageNumber = 1, int pageSize = 10, string? search = null, string? city = null, string? sportType = null, bool? isApproved = null)
        {
            var result = await _turfService.GetAllTurfsPagedAsync(pageNumber, pageSize, search, city, sportType, isApproved);
            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTurf(Guid id)
        {
            var result = await _turfService.ApproveTurfAsync(id);
            if (result.Success)
                TempData["Success"] = "Turf approved successfully.";
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Turfs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTurf(Guid id, string? reason = null)
        {
            var result = await _turfService.RejectTurfAsync(id, reason ?? "Rejected by admin");
            if (result.Success)
                TempData["Success"] = "Turf rejected.";
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Turfs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTurf(Guid id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var result = await _turfService.DeleteTurfAsync(id, userId, role);
            
            if (result.Success)
                TempData["Success"] = "Turf deleted successfully.";
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Turfs));
        }

        #endregion

        #region TurfOwner Management

        public async Task<IActionResult> TurfOwners(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var pagedOwners = await _unitOfWork.TurfOwners.GetPagedAsync(
                pageNumber,
                pageSize,
                o => string.IsNullOrEmpty(search) || o.BusinessName.Contains(search) || o.User.FullName.Contains(search),
                query => query.OrderByDescending(o => o.CreatedAt),
                "User"
            );

            return View(pagedOwners);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTurfOwner(int id)
        {
            var owner = await _unitOfWork.TurfOwners.FindAsync(o => o.UserId == id, includeProperties: "User");
            if (owner == null) return NotFound();

            var user = owner.User ?? await _unitOfWork.Users.FindAsync(u => u.UserId == id);
            
            // Check if owner has any turfs
            bool hasTurfs = await _unitOfWork.Turfs.GetCountAsync(t => t.OwnerId == id) > 0;
            if (hasTurfs)
            {
                // Soft delete by deactivating
                if (user != null)
                {
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Users.Update(user);
                }

                // Deactivate all their turfs
                var turfs = await _unitOfWork.Turfs.GetAllAsync();
                var ownerTurfs = turfs.Where(t => t.OwnerId == id).ToList();
                foreach (var turf in ownerTurfs)
                {
                    turf.IsActive = false;
                    turf.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Turfs.Update(turf);
                }

                await _unitOfWork.CompleteAsync();
                TempData["Success"] = $"Turf Owner '{user?.FullName ?? owner.BusinessName}' has active turfs. The account has been deactivated and all listings disabled to preserve database integrity.";
                return RedirectToAction(nameof(TurfOwners));
            }

            // If no turfs, we can delete the TurfOwner profile and the User account
            _unitOfWork.TurfOwners.Delete(owner);
            if (user != null)
            {
                _unitOfWork.Users.Delete(user);
            }
            
            await _unitOfWork.CompleteAsync();
            TempData["Success"] = "Turf Owner and user account deleted successfully.";
            return RedirectToAction(nameof(TurfOwners));
        }

        [HttpGet]
        public async Task<IActionResult> KycQueue(int pageNumber = 1, int pageSize = 10)
        {
            var pendingKyc = await _unitOfWork.TurfOwners.GetPagedAsync(
                pageNumber,
                pageSize,
                o => o.VerificationStatus == turf_management_system.Models.Enums.VerificationStatus.Submitted,
                query => query.OrderBy(o => o.SubmittedAt),
                "User"
            );

            return View(pendingKyc);
        }

        [HttpGet]
        public async Task<IActionResult> ReviewKyc(int id)
        {
            var owner = await _unitOfWork.TurfOwners.GetByIdAsync(id);
            if (owner == null) return NotFound();

            // Load user data explicitly if not loaded by eager loading
            var user = await _unitOfWork.Users.GetByIdAsync(owner.UserId);
            owner.User = user!;

            return View(owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessKyc(int id, bool approve, string? reason)
        {
            var owner = await _unitOfWork.TurfOwners.GetByIdAsync(id);
            if (owner == null) return NotFound();

            if (approve)
            {
                owner.VerificationStatus = turf_management_system.Models.Enums.VerificationStatus.Approved;
                owner.AdminComments = null;
                TempData["Success"] = "Turf Owner has been approved.";
            }
            else
            {
                owner.VerificationStatus = turf_management_system.Models.Enums.VerificationStatus.Rejected;
                owner.AdminComments = reason ?? "Your documents were rejected. Please re-upload valid documents.";
                TempData["Success"] = "Turf Owner application rejected.";
            }

            await _unitOfWork.CompleteAsync();
            return RedirectToAction(nameof(KycQueue));
        }

        #endregion
    }
}
