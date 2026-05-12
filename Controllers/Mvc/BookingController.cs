using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Hubs;
using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers.Mvc
{
    [Authorize(Roles = "User,TurfOwner,TurfManager")]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BookingHubNotifier _hubNotifier;

        public BookingController(IBookingService bookingService, IUnitOfWork unitOfWork, BookingHubNotifier hubNotifier)
        {
            _bookingService = bookingService;
            _unitOfWork = unitOfWork;
            _hubNotifier = hubNotifier;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ── Browse Turfs ──────────────────────────────────────────────────────────
        [AllowAnonymous]
        public async Task<IActionResult> Browse(string? city, string? sportType, string? search)
        {
            var cities = await _unitOfWork.Turfs.GetDistinctCitiesAsync();
            var sportTypes = await _unitOfWork.Turfs.GetDistinctSportTypesAsync();

            var (turfs, _) = await _unitOfWork.Turfs.GetAllPagedAsync(1, 50, search, city, sportType, true);

            ViewBag.Cities = cities;
            ViewBag.SportTypes = sportTypes;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedSport = sportType;
            ViewBag.Search = search;

            return View(turfs);
        }

        // ── Select Slot ───────────────────────────────────────────────────────────
        [AllowAnonymous]
        public async Task<IActionResult> SelectSlot(Guid turfId, string? date)
        {
            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(turfId);
            if (turf == null || !turf.IsApproved || !turf.IsActive) return NotFound();

            // Default to tomorrow if no date specified
            var bookingDate = string.IsNullOrEmpty(date)
                ? DateOnly.FromDateTime(DateTime.Today.AddDays(1))
                : DateOnly.Parse(date);

            var slots = await _bookingService.GetAvailableSlotsAsync(turfId, bookingDate);
            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == turfId);

            ViewBag.BookingDate = bookingDate;
            ViewBag.Config = config;

            return View((turf, slots));
        }

        // ── AJAX: Get Slots for Date ───────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("api/booking/slots/{turfId}")]
        public async Task<IActionResult> GetSlots(Guid turfId, string date)
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
                return BadRequest("Invalid date format.");

            var slots = await _bookingService.GetAvailableSlotsAsync(turfId, parsedDate);
            return Json(slots);
        }

        // ── Lock Slot + Create Booking ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockSlot(Guid turfId, Guid slotId, string bookingDate, string? specialRequest)
        {
            if (!DateOnly.TryParse(bookingDate, out var parsedDate))
            {
                TempData["Error"] = "Invalid date.";
                return RedirectToAction("SelectSlot", new { turfId });
            }

            var userId = GetUserId();
            var (success, message, bookingId) = await _bookingService.LockSlotAndCreateBookingAsync(
                turfId, slotId, parsedDate, userId, specialRequest);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction("SelectSlot", new { turfId, date = bookingDate });
            }

            // Notify other users via SignalR
            var slot = await _unitOfWork.TurfSlots.GetByIdAsync(slotId);
            if (slot != null)
            {
                await _hubNotifier.NotifySlotLocked(
                    turfId.ToString(), bookingDate, slotId.ToString(), DateTime.UtcNow.AddMinutes(5));
            }

            TempData["Success"] = message;
            return RedirectToAction("Payment", new { bookingId });
        }

        // ── Payment Page ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Payment(Guid bookingId)
        {
            var userId = GetUserId();
            var booking = await _bookingService.GetBookingWithDetailsAsync(bookingId, userId, "User");

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.PendingPayment)
            {
                TempData["Error"] = booking.Status == BookingStatus.Expired
                    ? "Your slot reservation has expired. Please book again."
                    : "This booking is no longer awaiting payment.";
                return RedirectToAction("MyBookings");
            }

            // Check lock is still active
            var slotLock = await _unitOfWork.SlotLocks.GetLockByBookingIdAsync(bookingId);
            if (slotLock == null || !slotLock.IsActive)
            {
                TempData["Error"] = "Your slot reservation has expired. Please book again.";
                return RedirectToAction("MyBookings");
            }

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == booking.TurfId);
            ViewBag.Config = config;
            ViewBag.LockExpiresAt = slotLock.LockedUntil;

            // Calculate required payment amount
            decimal requiredAmount = booking.TotalAmount;
            if (config != null && !config.RequireFullPayment)
                requiredAmount = Math.Round(booking.TotalAmount * config.AdvancePaymentPercent / 100, 2);

            ViewBag.RequiredAmount = requiredAmount;

            return View(booking);
        }

        // ── Submit Payment ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPayment(Guid bookingId, string transactionId,
            PaymentMethod paymentMethod, decimal amount, PaymentType paymentType)
        {
            var userId = GetUserId();
            var (success, message) = await _bookingService.SubmitPaymentAsync(
                bookingId, userId, transactionId, paymentMethod, amount, paymentType);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction("Payment", new { bookingId });
            }

            TempData["Success"] = message;
            return RedirectToAction("PaymentPending", new { bookingId });
        }

        // ── Payment Pending page ──────────────────────────────────────────────────
        public async Task<IActionResult> PaymentPending(Guid bookingId)
        {
            var userId = GetUserId();
            var booking = await _bookingService.GetBookingWithDetailsAsync(bookingId, userId, "User");
            if (booking == null) return NotFound();
            return View(booking);
        }

        // ── Booking Confirmation ──────────────────────────────────────────────────
        [AllowAnonymous]
        public async Task<IActionResult> Confirmation(Guid bookingId)
        {
            var userId = User.Identity?.IsAuthenticated == true ? (int?)GetUserId() : null;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";
            var booking = await _bookingService.GetBookingWithDetailsAsync(bookingId, userId ?? 0, role);
            if (booking == null) return NotFound();
            return View(booking);
        }

        // ── My Bookings ───────────────────────────────────────────────────────────
        public async Task<IActionResult> MyBookings(int pageNumber = 1, BookingStatus? status = null)
        {
            var userId = GetUserId();
            var (items, totalCount) = await _bookingService.GetMyBookingsAsync(userId, pageNumber, 10, status);

            ViewBag.TotalCount = totalCount;
            ViewBag.PageNumber = pageNumber;
            ViewBag.SelectedStatus = status;

            return View(items);
        }

        // ── Cancel Booking ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid bookingId, string? reason)
        {
            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var (success, message) = await _bookingService.CancelBookingAsync(bookingId, userId, role);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("MyBookings");
        }

        // ── Notification Bell (partial) ───────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetUserId();
            var notifications = await _unitOfWork.Notifications.GetAllAsync();
            var userNotifs = notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToList();
            return Json(userNotifs);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(Guid notificationId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification != null && notification.UserId == GetUserId())
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.CompleteAsync();
            }
            return Ok();
        }
    }
}
