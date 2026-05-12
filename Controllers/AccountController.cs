using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            var roles = await _unitOfWork.Roles.GetAllAsync();
            // Show all roles EXCEPT Platform Admins (Level 0 and 1)
            ViewBag.Roles = roles.Where(r => turf_management_system.Models.Logic.RoleHierarchy.GetRoleLevel(r.RoleName) >= 2).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(model.RoleId);
                if (role == null || turf_management_system.Models.Logic.RoleHierarchy.GetRoleLevel(role.RoleName) < 2)
                {
                    ModelState.AddModelError("RoleId", "Invalid role selection.");
                    var roles = await _unitOfWork.Roles.GetAllAsync();
                    ViewBag.Roles = roles.Where(r => turf_management_system.Models.Logic.RoleHierarchy.GetRoleLevel(r.RoleName) >= 2).ToList();
                    return View(model);
                }

                var existingUser = await _unitOfWork.Users.GetByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    PhoneNumber = model.PhoneNumber,
                    RoleId = model.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CompleteAsync();

                // If TurfOwner, create profile
                if (role?.RoleName == "TurfOwner")
                {
                    var turfOwner = new TurfOwner
                    {
                        UserId = user.UserId,
                        BusinessName = model.BusinessName ?? "My Turf Business",
                        BusinessAddress = model.BusinessAddress,
                        ContactNumber = model.ContactNumber ?? model.PhoneNumber,
                        VerificationStatus = turf_management_system.Models.Enums.VerificationStatus.Pending,
                        NationalIdNumber = model.NationalIdNumber,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.TurfOwners.AddAsync(turfOwner);
                    await _unitOfWork.CompleteAsync();
                }

                TempData["Success"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("TurfOwner"))
                    return RedirectToAction("Dashboard", "TurfOwner");
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _unitOfWork.Users.GetByEmailAsync(model.Email);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    if (!user.IsActive)
                    {
                        ModelState.AddModelError("", "Your account is inactive. Please contact admin.");
                        return View(model);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Role, user.Role.RoleName)
                    };

                    // Add Scoped Claims
                    if (user.Role.RoleName == "TurfOwner")
                    {
                        // TurfOwner acts as the owner of their Turfs, so we can use NameIdentifier as OwnerId in queries
                        claims.Add(new Claim("OwnerId", user.UserId.ToString()));
                    }
                    else if (new[] { "TurfManager", "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard" }.Contains(user.Role.RoleName))
                    {
                        var staffProfile = await _unitOfWork.StaffProfiles.FindAsync(s => s.UserId == user.UserId && s.IsActive);
                        if (staffProfile != null)
                        {
                            claims.Add(new Claim("TurfId", staffProfile.TurfId.ToString()));
                        }
                        else
                        {
                            ModelState.AddModelError("", "Your staff profile is inactive or not assigned to a turf.");
                            return View(model);
                        }
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Professional Redirection Logic
                    var roleName = user.Role.RoleName;
                    
                    if (turf_management_system.Models.Logic.RoleHierarchy.GetRoleLevel(roleName) <= 1)
                    {
                        // Platforms Admins (SuperAdmin, Admin, Support, etc.)
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else if (roleName == "TurfOwner" || roleName == "TurfManager")
                    {
                        // Turf Management
                        return RedirectToAction("Dashboard", "TurfOwner");
                    }
                    else if (new[] { "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard" }.Contains(roleName))
                    {
                        // Staff - Should probably have their own dashboard eventually
                        return RedirectToAction("Dashboard", "TurfOwner");
                    }
                    else
                    {
                        // Customers/Users
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Invalid email or password.");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AutoLogin(string role)
        {
            var email = role switch
            {
                "SuperAdmin" => "superadmin@turf.com",
                "Admin" => "admin@turf.com",
                "TurfOwner" => "owner@turf.com",
                "TurfManager" => "manager@turf.com",
                "Receptionist" => "receptionist@turf.com",
                "User" => "user@turf.com",
                _ => null
            };

            if (email == null) return RedirectToAction("Login");

            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role.RoleName)
                };

                if (user.Role.RoleName == "TurfOwner") claims.Add(new Claim("OwnerId", user.UserId.ToString()));
                else if (new[] { "TurfManager", "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard" }.Contains(user.Role.RoleName))
                {
                    var staffProfile = await _unitOfWork.StaffProfiles.FindAsync(s => s.UserId == user.UserId && s.IsActive);
                    if (staffProfile != null) claims.Add(new Claim("TurfId", staffProfile.TurfId.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // Professional Redirection Logic
                var roleName = user.Role.RoleName;

                if (turf_management_system.Models.Logic.RoleHierarchy.GetRoleLevel(roleName) <= 1)
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (roleName == "TurfOwner" || roleName == "TurfManager" || new[] { "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard" }.Contains(roleName))
                {
                    return RedirectToAction("Dashboard", "TurfOwner");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["Error"] = $"Test user for role {role} ({email}) does not exist yet.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize]
        public IActionResult MyBookings()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
