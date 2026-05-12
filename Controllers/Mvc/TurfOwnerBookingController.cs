using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Controllers.Mvc
{
    [Authorize(Roles = "TurfOwner,TurfManager")]
    public class TurfOwnerBookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IUnitOfWork _unitOfWork;

        public TurfOwnerBookingController(IBookingService bookingService, IUnitOfWork unitOfWork)
        {
            _bookingService = bookingService;
            _unitOfWork = unitOfWork;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ── All Bookings for Owner ────────────────────────────────────────────────
        public async Task<IActionResult> Bookings(int pageNumber = 1, BookingStatus? status = null)
        {
            var ownerId = GetUserId();
            var (items, totalCount) = await _bookingService.GetOwnerAllBookingsAsync(ownerId, pageNumber, 10, status);

            ViewBag.TotalCount = totalCount;
            ViewBag.PageNumber = pageNumber;
            ViewBag.SelectedStatus = status;

            return View(items);
        }

        // ── Booking Config ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> BookingConfig(Guid turfId)
        {
            var ownerId = GetUserId();
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);

            if (turf == null || turf.OwnerId != ownerId)
                return Forbid();

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == turfId)
                         ?? new TurfBookingConfig { TurfId = turfId };

            ViewBag.TurfName = turf.Name;
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBookingConfig(TurfBookingConfig model)
        {
            var ownerId = GetUserId();
            var turf = await _unitOfWork.Turfs.GetByIdAsync(model.TurfId);

            if (turf == null || turf.OwnerId != ownerId)
                return Forbid();

            var existing = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == model.TurfId);
            if (existing == null)
            {
                model.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.BookingConfigs.AddAsync(model);
            }
            else
            {
                existing.AvailableDaysMask = model.AvailableDaysMask;
                existing.OpeningTime = model.OpeningTime;
                existing.ClosingTime = model.ClosingTime;
                existing.SlotDurationMinutes = model.SlotDurationMinutes;
                existing.MaxAdvanceBookingDays = model.MaxAdvanceBookingDays;
                existing.RequireFullPayment = model.RequireFullPayment;
                existing.AdvancePaymentPercent = model.AdvancePaymentPercent;
                existing.AcceptBkash = model.AcceptBkash;
                existing.AcceptNagad = model.AcceptNagad;
                existing.AcceptRocket = model.AcceptRocket;
                existing.CancellationAllowed = model.CancellationAllowed;
                existing.RefundPercent = model.RefundPercent;
                existing.CancellationDeadlineHours = model.CancellationDeadlineHours;
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.BookingConfigs.Update(existing);
            }

            await _unitOfWork.CompleteAsync();
            TempData["Success"] = "Booking configuration saved successfully.";
            return RedirectToAction("BookingConfig", new { turfId = model.TurfId });
        }

        // ── Pending Payments ──────────────────────────────────────────────────────
        public async Task<IActionResult> PendingPayments()
        {
            var ownerId = GetUserId();
            var payments = await _bookingService.GetPendingPaymentsForOwnerAsync(ownerId);
            return View(payments);
        }

        // ── Verify Payment ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VerifyPayment(Guid paymentId)
        {
            var ownerId = GetUserId();
            var payment = await _unitOfWork.Payments.FindAsync(
                p => p.Id == paymentId, includeProperties: "Booking,Booking.Turf,Booking.User,User");

            if (payment == null) return NotFound();
            if (payment.Booking.Turf.OwnerId != ownerId) return Forbid();

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(Guid paymentId)
        {
            var ownerId = GetUserId();
            var (success, message) = await _bookingService.VerifyPaymentAsync(paymentId, ownerId);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("PendingPayments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(Guid paymentId, string reason)
        {
            var ownerId = GetUserId();
            var (success, message) = await _bookingService.RejectPaymentAsync(paymentId, ownerId, reason);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("PendingPayments");
        }

        // ── Cancel Booking (by Owner) ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(Guid bookingId, string? reason)
        {
            var ownerId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var (success, message) = await _bookingService.CancelBookingAsync(bookingId, ownerId, role);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("Bookings");
        }
    }
}
