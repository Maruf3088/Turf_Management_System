using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;
using turf_management_system.DTOs.Turf;
using turf_management_system.Models.Enums;
using turf_management_system.Models.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace turf_management_system.Controllers
{
    [Authorize(Roles = "TurfOwner")]
    public class TurfOwnerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITurfService _turfService;
        private readonly IWebHostEnvironment _env;

        public TurfOwnerController(IUnitOfWork unitOfWork, ITurfService turfService, IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _turfService = turfService;
            _env = env;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            
            var userId = int.Parse(userIdStr);
            var fullName = User.FindFirstValue(ClaimTypes.Name) ?? "Turf Owner";
            
            var myTurfsCount = await _unitOfWork.Turfs.GetCountAsync(t => t.OwnerId == userId);
            var totalBookingsCount = await _unitOfWork.Bookings.GetCountAsync(b => b.Turf.OwnerId == userId);
            var todaysDate = DateOnly.FromDateTime(DateTime.Today);
            var todaysBookingsCount = await _unitOfWork.Bookings.GetCountAsync(b => 
                b.Turf.OwnerId == userId && b.BookingDate == todaysDate);

            var owner = await _unitOfWork.TurfOwners.GetByIdAsync(userId);
            
            // Fetch recent bookings using the improved repository method
            var (recentBookings, _) = await _unitOfWork.Bookings.GetPagedAsync(1, 5, null, null, null, userId);

            // Fetch revenue and pending/upcoming records
            var payments = await _unitOfWork.Payments.GetAllAsync(includeProperties: "Booking,Booking.Turf,User");
            var ownerPayments = payments.Where(p => p.Booking?.Turf?.OwnerId == userId).ToList();

            var totalRevenue = ownerPayments
                .Where(p => p.Status == PaymentVerificationStatus.Verified)
                .Sum(p => p.Amount);

            var pendingRevenue = ownerPayments
                .Where(p => p.Status == PaymentVerificationStatus.Pending)
                .Sum(p => p.Amount);

            var pendingPayments = ownerPayments
                .Where(p => p.Status == PaymentVerificationStatus.Pending)
                .OrderByDescending(p => p.SubmittedAt)
                .ToList();

            var (allBookings, _) = await _unitOfWork.Bookings.GetPagedAsync(1, 100, null, null, BookingStatus.Confirmed, userId);
            var upcomingBookings = allBookings
                .Where(b => b.BookingDate >= todaysDate)
                .OrderBy(b => b.BookingDate)
                .ThenBy(b => b.StartTime)
                .ToList();

            var viewModel = new TurfOwnerDashboardVM
            {
                FullName = fullName,
                MyTurfs = myTurfsCount,
                TodaysBookings = todaysBookingsCount,
                TotalBookings = totalBookingsCount,
                IsActive = true,
                VerificationStatus = owner?.VerificationStatus ?? VerificationStatus.Pending,
                RecentBookings = recentBookings.ToList(),
                TotalRevenue = totalRevenue,
                PendingRevenue = pendingRevenue,
                RecentPayments = pendingPayments,
                UpcomingBookings = upcomingBookings
            };

            return View(viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _turfService.GetTurfByIdAsync(id);
            if (!result.Success) return NotFound();

            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (result.Data!.OwnerId != ownerId) return Forbid();

            var turf = result.Data;
            var dto = new UpdateTurfDto
            {
                Name = turf.Name,
                Description = turf.Description,
                Location = turf.Location,
                City = turf.City,
                PricePerHour = turf.PricePerHour,
                MorningPricePerHour = turf.MorningPricePerHour,
                EveningPricePerHour = turf.EveningPricePerHour,
                SportType = turf.SportType,
                TurfSize = turf.TurfSize,
                Amenities = turf.Amenities,
                IndoorOutdoor = turf.IndoorOutdoor,
                ContactNumber = turf.ContactNumber
            };

            ViewBag.TurfId = id;
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateTurfDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (!ModelState.IsValid)
            {
                ViewBag.TurfId = id;
                return View(dto);
            }

            var result = await _turfService.UpdateTurfAsync(id, dto, ownerId);
            if (result.Success)
            {
                TempData["Success"] = "Turf details updated successfully!";
                return RedirectToAction(nameof(MyTurfs));
            }

            ModelState.AddModelError("", result.Message ?? "Failed to update turf.");
            ViewBag.TurfId = id;
            return View(dto);
        }

        public async Task<IActionResult> MyTurfs()
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _turfService.GetMyTurfsAsync(ownerId);
            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var owner = await _unitOfWork.TurfOwners.GetByIdAsync(ownerId);
            
            if (owner?.VerificationStatus != VerificationStatus.Approved)
            {
                TempData["Error"] = "Your account must be verified before you can list a turf.";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(new CreateTurfDto());
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
        public async Task<IActionResult> KycVerification()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var owner = await _unitOfWork.TurfOwners.GetByIdAsync(userId);
            
            if (owner == null) return NotFound();

            if (owner.VerificationStatus == VerificationStatus.Approved || owner.VerificationStatus == VerificationStatus.Submitted)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            ViewBag.Status = owner.VerificationStatus;
            ViewBag.Comments = owner.AdminComments;

            return View(new KycUploadVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KycVerification(KycUploadVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var owner = await _unitOfWork.TurfOwners.GetByIdAsync(userId);
            
            if (owner == null) return NotFound();

            try
            {
                owner.NidFrontImagePath = await SaveFileAsync(model.NidFrontImage!, "kyc");
                owner.NidBackImagePath = await SaveFileAsync(model.NidBackImage!, "kyc");
                owner.UtilityBillImagePath = await SaveFileAsync(model.UtilityBillImage!, "kyc");
                
                if (model.TradeLicenseImage != null)
                {
                    owner.TradeLicenseImagePath = await SaveFileAsync(model.TradeLicenseImage, "kyc");
                }

                owner.VerificationStatus = VerificationStatus.Submitted;
                owner.SubmittedAt = DateTime.UtcNow;
                owner.AdminComments = null; // Clear previous comments

                await _unitOfWork.CompleteAsync();
                TempData["Success"] = "KYC documents submitted successfully. Pending admin approval.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error uploading files: " + ex.Message);
                return View(model);
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return string.Empty;

            var wwwRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(wwwRootPath))
            {
                // Fallback: check if we are running in bin/Debug and try to find the project wwwroot
                var contentRoot = _env.ContentRootPath;
                wwwRootPath = Path.Combine(contentRoot, "wwwroot");
                
                if (!Directory.Exists(wwwRootPath))
                {
                    // Try to go up from bin/Debug/net8.0 to find the project root
                    var parentDir = Directory.GetParent(contentRoot)?.Parent?.Parent?.FullName;
                    if (parentDir != null && Directory.Exists(Path.Combine(parentDir, "wwwroot")))
                    {
                        wwwRootPath = Path.Combine(parentDir, "wwwroot");
                    }
                }
            }

            var uploadsFolder = Path.Combine(wwwRootPath, "uploads", folder);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileExt = Path.GetExtension(file.FileName);
            var uniqueFileName = Guid.NewGuid().ToString() + fileExt;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{uniqueFileName}";
        }
    }
}
