using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Models.ViewModels;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context; // for transactions
        private readonly IExternalNotificationService _externalNotificationService;

        public BookingService(IUnitOfWork unitOfWork, AppDbContext context, IExternalNotificationService externalNotificationService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _externalNotificationService = externalNotificationService;
        }

        // ────────────────────────────────────────────────────────────────────────────
        // STEP 1: Lock Slot + Create Booking
        // ────────────────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, Guid? BookingId)> LockSlotAndCreateBookingAsync(
            Guid turfId, Guid slotId, DateOnly bookingDate, int userId, string? specialRequest)
        {
            // ── Validate Turf ────────────────────────────────────────────────────
            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(turfId);
            if (turf == null || !turf.IsActive || turf.IsDeleted)
                return (false, "Turf is not available.", null);

            if (!turf.IsApproved)
                return (false, "This turf has not been verified by admin yet.", null);

            // ── Validate Slot ────────────────────────────────────────────────────
            var slot = await _unitOfWork.TurfSlots.GetByIdAsync(slotId);
            if (slot == null || slot.TurfId != turfId || !slot.IsAvailable)
                return (false, "Selected slot is not valid.", null);

            if (slot.EffectiveFromDate.HasValue && slot.EffectiveFromDate > bookingDate)
                return (false, "Selected slot is not active yet.", null);

            if (slot.EffectiveToDate.HasValue && slot.EffectiveToDate <= bookingDate)
                return (false, "Selected slot has expired.", null);

            // ── Validate Booking Config Rules ─────────────────────────────────────
            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == turfId);
            if (config != null)
            {
                // Check if day is allowed
                if (!config.IsDayAvailable(bookingDate.DayOfWeek))
                    return (false, $"This turf is not open on {bookingDate.DayOfWeek}.", null);

                // Check advance booking limit
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var daysAhead = bookingDate.DayNumber - today.DayNumber;
                if (daysAhead < 0)
                    return (false, "Cannot book a date in the past.", null);
                if (daysAhead > config.MaxAdvanceBookingDays)
                    return (false, $"This turf only allows booking up to {config.MaxAdvanceBookingDays} days in advance.", null);

                // Check if slot is within operating hours
                if (slot.StartTime < config.OpeningTime || slot.EndTime > config.ClosingTime)
                    return (false, "This slot is outside the turf's operating hours.", null);
            }
            else
            {
                // No config = check basic past date rule
                if (bookingDate < DateOnly.FromDateTime(DateTime.UtcNow))
                    return (false, "Cannot book a date in the past.", null);
            }

            // ── Atomic Slot Lock + Booking Creation (DB Transaction) ─────────────
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check for any ACTIVE lock overlapping this slot time range
                var existingLock = await _unitOfWork.SlotLocks.GetActiveLockAsync(
                    turfId, bookingDate, slot.StartTime, slot.EndTime);

                if (existingLock != null)
                    return (false, "This slot is currently being reserved by another user. Please try again in a moment.", null);

                // Check for an existing confirmed/pending booking on same slot and date
                bool alreadyBooked = await _unitOfWork.Bookings.IsSlotAlreadyBookedAsync(slotId, bookingDate);
                if (alreadyBooked)
                    return (false, "This slot has already been booked for the selected date.", null);

                // Calculate price
                var totalHours = (decimal)(slot.EndTime - slot.StartTime).TotalHours;
                decimal hourlyRate = slot.PricingVariant == "Evening"
                    ? (turf.EveningPricePerHour > 0 ? turf.EveningPricePerHour : turf.PricePerHour)
                    : (turf.MorningPricePerHour > 0 ? turf.MorningPricePerHour : turf.PricePerHour);
                var totalAmount = totalHours * hourlyRate;

                // Create the booking
                var bookingId = Guid.NewGuid();

                var slotLock = new SlotLock
                {
                    TurfId = turfId,
                    BookingDate = bookingDate,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    LockedByUserId = userId,
                    BookingId = bookingId,
                    LockedUntil = DateTime.UtcNow.AddMinutes(5),
                    IsReleased = false,
                    CreatedAt = DateTime.UtcNow
                };

                var booking = new Booking
                {
                    Id = bookingId,
                    TurfId = turfId,
                    UserId = userId,
                    SlotId = slotId,
                    BookingDate = bookingDate,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    TotalHours = totalHours,
                    TotalAmount = totalAmount,
                    AmountPaid = 0,
                    Status = BookingStatus.PendingPayment,
                    PaymentStatus = PaymentStatus.Unpaid,
                    SpecialRequest = specialRequest,
                    SlotLockId = slotLock.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.SlotLocks.AddAsync(slotLock);
                await _unitOfWork.Bookings.AddAsync(booking);
                await _unitOfWork.CompleteAsync();

                // Audit log
                await WriteAuditLogAsync("Booking", bookingId.ToString(), "Created",
                    null, $"Status: PendingPayment, Amount: {totalAmount}", userId);

                // Notify user
                await SendNotificationAsync(userId, "Slot Reserved!",
                    $"Your slot at {turf.Name} on {bookingDate:dd MMM yyyy} ({slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}) has been reserved for 5 minutes. Please complete payment.",
                    NotificationType.SlotLocked,
                    $"/Booking/Payment/{bookingId}");

                await transaction.CommitAsync();
                return (true, "Slot reserved! Please complete payment within 5 minutes.", bookingId);
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "An error occurred while reserving the slot. Please try again.", null);
            }
        }

        // ────────────────────────────────────────────────────────────────────────────
        // STEP 2: Submit Payment
        // ────────────────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> SubmitPaymentAsync(
            Guid bookingId, int userId, string transactionId, PaymentMethod paymentMethod, decimal amount, PaymentType paymentType, bool autoVerify = false, bool skipAmountValidation = false)
        {
            var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
            if (booking == null)
                return (false, "Booking not found.");

            if (booking.UserId != userId)
                return (false, "Unauthorized.");

            if (booking.Status != BookingStatus.PendingPayment)
                return (false, "This booking is not awaiting payment.");

            // Check if slot lock is still active
            var slotLock = await _unitOfWork.SlotLocks.GetLockByBookingIdAsync(bookingId);
            if (slotLock == null || !slotLock.IsActive)
            {
                // Expire the booking if the lock timed out
                booking.Status = BookingStatus.Expired;
                booking.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Bookings.Update(booking);
                await _unitOfWork.CompleteAsync();
                return (false, "Your slot reservation has expired. Please start the booking process again.");
            }

            // Prevent duplicate transaction IDs securely while allowing batch submissions from the same user
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                var txTrimmed = transactionId.Trim();
                
                // A verified transaction ID can never be reused by anyone (including the same user)
                var alreadyVerified = await _unitOfWork.Payments.FindAsync(p => 
                    p.TransactionId == txTrimmed && 
                    p.Status == PaymentVerificationStatus.Verified);
                if (alreadyVerified != null)
                {
                    return (false, "This transaction ID has already been verified and used.");
                }

                // A pending transaction ID from a DIFFERENT user cannot be used
                var pendingFromOther = await _unitOfWork.Payments.FindAsync(p => 
                    p.TransactionId == txTrimmed && 
                    p.UserId != userId && 
                    p.Status == PaymentVerificationStatus.Pending);
                if (pendingFromOther != null)
                {
                    return (false, "This transaction ID is already pending verification for another user.");
                }
            }

            // Validate amount (skipped for proportional batch payments where the total was already validated by the caller)
            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == booking.TurfId);
            if (!skipAmountValidation)
            {
                decimal requiredAmount;
                if (config != null && !config.RequireFullPayment)
                {
                    requiredAmount = Math.Round(booking.TotalAmount * config.AdvancePaymentPercent / 100, 2);
                }
                else
                {
                    requiredAmount = booking.TotalAmount;
                }

                if (amount < requiredAmount)
                    return (false, $"Insufficient payment amount. Required: ৳{requiredAmount:F2}");
            }

            var payment = new Payment
            {
                BookingId = bookingId,
                UserId = userId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId.Trim(),
                Status = autoVerify ? PaymentVerificationStatus.Verified : PaymentVerificationStatus.Pending,
                PaymentType = paymentType,
                SubmittedAt = DateTime.UtcNow,
                VerifiedAt = autoVerify ? DateTime.UtcNow : null,
                VerifiedByAdminId = autoVerify ? booking.Turf.OwnerId : null
            };

            await _unitOfWork.Payments.AddAsync(payment);

            if (autoVerify)
            {
                booking.AmountPaid += amount;
                booking.Status = BookingStatus.Confirmed;
                booking.ConfirmedAt = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;

                booking.PaymentStatus = booking.AmountPaid >= booking.TotalAmount
                    ? PaymentStatus.FullyPaid
                    : PaymentStatus.PartiallyPaid;

                if (slotLock != null)
                {
                    slotLock.IsReleased = true;
                    _unitOfWork.SlotLocks.Update(slotLock);
                }

                _unitOfWork.Bookings.Update(booking);

                // Notify user of instant confirmation
                await SendNotificationAsync(userId, "Booking Confirmed!",
                    $"Your booking at {booking.Turf.Name} on {booking.BookingDate:dd MMM yyyy} is CONFIRMED!",
                    NotificationType.BookingConfirmed,
                    $"/Booking/Details/{bookingId}");

                await WriteAuditLogAsync("Payment", payment.Id.ToString(), "VerifiedAuto",
                    null, $"TxId: {transactionId}, Amount: {amount}, Method: {paymentMethod} (Simulated Gateway)", userId);
            }
            else
            {
                booking.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Bookings.Update(booking);

                // Notify turf owner
                await SendNotificationAsync(booking.Turf.OwnerId,
                    "New Payment Submitted",
                    $"Customer {booking.User.FullName} submitted payment of ৳{amount:F2} for booking #{bookingId.ToString()[..8].ToUpper()}. Please verify.",
                    NotificationType.PaymentSubmitted,
                    $"/TurfOwnerBooking/VerifyPayment/{payment.Id}");

                await WriteAuditLogAsync("Payment", payment.Id.ToString(), "Submitted",
                    null, $"TxId: {transactionId}, Amount: {amount}, Method: {paymentMethod}", userId);
            }

            await _unitOfWork.CompleteAsync();

            return (true, autoVerify
                ? "Payment verified instantly! Your booking is confirmed."
                : "Payment submitted successfully. Your booking will be confirmed after owner verification.");
        }


        // ────────────────────────────────────────────────────────────────────────────
        // STEP 3: Verify Payment (Owner/Admin)
        // ────────────────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> VerifyPaymentAsync(Guid paymentId, int verifierUserId)
        {
            var payment = await _unitOfWork.Payments.FindAsync(
                p => p.Id == paymentId, includeProperties: "Booking,Booking.Turf,User");

            if (payment == null)
                return (false, "Payment not found.");

            if (payment.Status != PaymentVerificationStatus.Pending)
                return (false, "This payment has already been processed.");

            // Authorization: verifier must own the turf
            if (payment.Booking.Turf.OwnerId != verifierUserId)
                return (false, "You are not authorized to verify this payment.");

            // Find all pending payments submitted by the SAME user with the SAME transaction ID
            var sharedPayments = new List<Payment>();
            if (!string.IsNullOrWhiteSpace(payment.TransactionId))
            {
                var allPayments = await _unitOfWork.Payments.GetAllAsync(includeProperties: "Booking,Booking.Turf,User");
                sharedPayments = allPayments.Where(p => 
                    p.TransactionId == payment.TransactionId && 
                    p.UserId == payment.UserId && 
                    p.Status == PaymentVerificationStatus.Pending &&
                    p.Booking != null &&
                    p.Booking.Turf != null &&
                    p.Booking.Turf.OwnerId == verifierUserId
                ).ToList();
            }

            if (!sharedPayments.Any(p => p.Id == payment.Id))
            {
                sharedPayments.Add(payment);
            }

            foreach (var p in sharedPayments)
            {
                p.Status = PaymentVerificationStatus.Verified;
                p.VerifiedAt = DateTime.UtcNow;
                p.VerifiedByAdminId = verifierUserId;
                _unitOfWork.Payments.Update(p);

                // Update booking
                var booking = p.Booking;
                booking.AmountPaid += p.Amount;
                booking.Status = BookingStatus.Confirmed;
                booking.ConfirmedAt = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;

                booking.PaymentStatus = booking.AmountPaid >= booking.TotalAmount
                    ? PaymentStatus.FullyPaid
                    : PaymentStatus.PartiallyPaid;

                // Release the slot lock
                var slotLock = await _unitOfWork.SlotLocks.GetLockByBookingIdAsync(booking.Id);
                if (slotLock != null)
                {
                    slotLock.IsReleased = true;
                    _unitOfWork.SlotLocks.Update(slotLock);
                }

                _unitOfWork.Bookings.Update(booking);

                // Notify customer
                await SendNotificationAsync(booking.UserId,
                    "Booking Confirmed! ✅",
                    $"Your booking at {booking.Turf.Name} on {booking.BookingDate:dd MMM yyyy} has been confirmed. Booking ID: #{booking.Id.ToString()[..8].ToUpper()}",
                    NotificationType.BookingConfirmed,
                    $"/Booking/Confirmation/{booking.Id}");

                await WriteAuditLogAsync("Booking", booking.Id.ToString(), "Confirmed",
                    "PendingPayment", "Confirmed", verifierUserId);
            }

            await _unitOfWork.CompleteAsync();

            return (true, $"{sharedPayments.Count} payment(s) verified and booking(s) confirmed successfully.");
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Reject Payment
        // ────────────────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> RejectPaymentAsync(Guid paymentId, int verifierUserId, string reason)
        {
            var payment = await _unitOfWork.Payments.FindAsync(
                p => p.Id == paymentId, includeProperties: "Booking,Booking.Turf,User");

            if (payment == null) return (false, "Payment not found.");
            if (payment.Booking.Turf.OwnerId != verifierUserId) return (false, "Unauthorized.");
            if (payment.Status != PaymentVerificationStatus.Pending) return (false, "Payment already processed.");

            // Find all pending payments submitted by the SAME user with the SAME transaction ID
            var sharedPayments = new List<Payment>();
            if (!string.IsNullOrWhiteSpace(payment.TransactionId))
            {
                var allPayments = await _unitOfWork.Payments.GetAllAsync(includeProperties: "Booking,Booking.Turf,User");
                sharedPayments = allPayments.Where(p => 
                    p.TransactionId == payment.TransactionId && 
                    p.UserId == payment.UserId && 
                    p.Status == PaymentVerificationStatus.Pending &&
                    p.Booking != null &&
                    p.Booking.Turf != null &&
                    p.Booking.Turf.OwnerId == verifierUserId
                ).ToList();
            }

            if (!sharedPayments.Any(p => p.Id == payment.Id))
            {
                sharedPayments.Add(payment);
            }

            foreach (var p in sharedPayments)
            {
                p.Status = PaymentVerificationStatus.Failed;
                p.RejectionReason = reason;
                p.VerifiedByAdminId = verifierUserId;
                p.VerifiedAt = DateTime.UtcNow;
                _unitOfWork.Payments.Update(p);

                // Notify customer
                await SendNotificationAsync(p.Booking.UserId,
                    "Payment Rejected",
                    $"Your payment for booking #{p.Booking.Id.ToString()[..8].ToUpper()} was rejected. Reason: {reason}. Please resubmit with correct details.",
                    NotificationType.PaymentFailed,
                    $"/Booking/Payment/{p.BookingId}");
            }

            await _unitOfWork.CompleteAsync();

            return (true, $"{sharedPayments.Count} payment(s) rejected. Customer has been notified.");
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Cancel Booking
        // ────────────────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)> CancelBookingAsync(Guid bookingId, int requesterId, string requesterRole)
        {
            var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return (false, "Booking not found.");

            bool isAdmin = turf_management_system.Models.Logic.RoleHierarchy.GetRoleLevel(requesterRole) <= 1;
            bool isOwner = booking.Turf.OwnerId == requesterId;
            bool isCustomer = booking.UserId == requesterId;

            if (!isAdmin && !isOwner && !isCustomer)
                return (false, "Unauthorized.");

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                return (false, "This booking cannot be cancelled.");

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == booking.TurfId);

            // Check cancellation deadline for customers
            if (isCustomer && !isAdmin)
            {
                if (config != null && !config.CancellationAllowed)
                    return (false, "Cancellations are not allowed for this turf.");

                var bookingDateTime = booking.BookingDate.ToDateTime(TimeOnly.FromTimeSpan(booking.StartTime));
                var hoursUntilBooking = (bookingDateTime - DateTime.UtcNow).TotalHours;

                if (config != null && hoursUntilBooking < config.CancellationDeadlineHours)
                    return (false, $"Cancellations must be made at least {config.CancellationDeadlineHours} hours before the booking time.");
            }

            var oldStatus = booking.Status.ToString();
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Bookings.Update(booking);

            // Release slot lock if active
            var slotLock = await _unitOfWork.SlotLocks.GetLockByBookingIdAsync(bookingId);
            if (slotLock != null && !slotLock.IsReleased)
            {
                slotLock.IsReleased = true;
                _unitOfWork.SlotLocks.Update(slotLock);
            }

            // Process refund if payment was made
            if (booking.AmountPaid > 0 && config != null)
            {
                var bookingDateTime = booking.BookingDate.ToDateTime(TimeOnly.FromTimeSpan(booking.StartTime));
                var hoursUntilBooking = (bookingDateTime - DateTime.UtcNow).TotalHours;

                decimal refundPercent = (hoursUntilBooking >= config.CancellationDeadlineHours)
                    ? config.RefundPercent
                    : 0;

                if (refundPercent > 0)
                {
                    var refundAmount = Math.Round(booking.AmountPaid * refundPercent / 100, 2);
                    booking.Status = BookingStatus.Refunded;
                    booking.PaymentStatus = PaymentStatus.Refunded;

                    await SendNotificationAsync(booking.UserId,
                        "Refund Initiated",
                        $"Your booking has been cancelled. A refund of ৳{refundAmount:F2} ({refundPercent}%) will be processed within 3-5 business days.",
                        NotificationType.RefundProcessed,
                        null);
                }
            }

            await _unitOfWork.CompleteAsync();

            await WriteAuditLogAsync("Booking", bookingId.ToString(), "Cancelled",
                oldStatus, "Cancelled", requesterId);

            return (true, "Booking cancelled successfully.");
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Get Available Slots
        // ────────────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<SlotAvailabilityVM>> GetAvailableSlotsAsync(Guid turfId, DateOnly date, int? currentUserId = null)
        {
            var turf = await _unitOfWork.Turfs.GetTurfWithDetailsAsync(turfId);
            if (turf == null) return Enumerable.Empty<SlotAvailabilityVM>();

            var config = await _unitOfWork.BookingConfigs.FindAsync(c => c.TurfId == turfId);

            // Filter by day-of-week if config exists and day is blocked
            if (config != null && !config.IsDayAvailable(date.DayOfWeek))
                return Enumerable.Empty<SlotAvailabilityVM>();

            var dayOfWeek = (int)date.DayOfWeek;
            var allSlots = turf.Slots
                .Where(s => (s.DayOfWeek == null || s.DayOfWeek == dayOfWeek) &&
                            (s.EffectiveFromDate == null || s.EffectiveFromDate <= date) &&
                            (s.EffectiveToDate == null || s.EffectiveToDate > date))
                .ToList();

            // Get existing confirmed/pending bookings on this date
            var existingBookings = await _unitOfWork.Bookings.GetBookingsForTurfOnDateAsync(turfId, date);
            var bookedSlotIds = existingBookings.Select(b => b.SlotId).ToHashSet();

            // Get active slot locks
            var activeLocks = (await _unitOfWork.SlotLocks.GetAllAsync())
                .Where(l => l.TurfId == turfId && l.BookingDate == date && l.IsActive)
                .ToList();

            var result = allSlots.Select(slot =>
            {
                SlotStatus status;
                if (!slot.IsAvailable)
                    status = SlotStatus.Unavailable;
                else if (bookedSlotIds.Contains(slot.Id))
                    status = SlotStatus.Booked;
                else
                {
                    var lockForSlot = activeLocks.FirstOrDefault(l => l.StartTime < slot.EndTime && l.EndTime > slot.StartTime);
                    if (lockForSlot != null)
                    {
                        if (currentUserId.HasValue && lockForSlot.LockedByUserId == currentUserId.Value)
                            status = SlotStatus.Selected;
                        else
                            status = SlotStatus.InProgress;
                    }
                    else
                    {
                        status = SlotStatus.Available;
                    }
                }

                var hours = (decimal)(slot.EndTime - slot.StartTime).TotalHours;
                
                // Calculate pricing based on Morning/Evening variant
                decimal hourlyRate = slot.PricingVariant == "Evening"
                    ? (turf.EveningPricePerHour > 0 ? turf.EveningPricePerHour : turf.PricePerHour)
                    : (turf.MorningPricePerHour > 0 ? turf.MorningPricePerHour : turf.PricePerHour);

                var startDateTime = DateTime.Today.Add(slot.StartTime);
                var endDateTime = DateTime.Today.Add(slot.EndTime);

                return new SlotAvailabilityVM
                {
                    SlotId = slot.Id,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    StartTimeDisplay = startDateTime.ToString("hh:mm tt"),
                    EndTimeDisplay = endDateTime.ToString("hh:mm tt"),
                    Price = hours * hourlyRate,
                    Status = status,
                    PricingVariant = slot.PricingVariant
                };
            }).OrderBy(s => s.StartTime).ToList();

            return result;
        }


        // ────────────────────────────────────────────────────────────────────────────
        // Queries
        // ────────────────────────────────────────────────────────────────────────────
        public async Task<Booking?> GetBookingWithDetailsAsync(Guid bookingId, int requesterId, string requesterRole)
        {
            var booking = await _unitOfWork.Bookings.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return null;

            bool isAdmin = turf_management_system.Models.Logic.RoleHierarchy.GetRoleLevel(requesterRole) <= 1;
            if (!isAdmin && booking.UserId != requesterId && booking.Turf.OwnerId != requesterId)
                return null;

            return booking;
        }

        public async Task<(IEnumerable<Booking> Items, int TotalCount)> GetMyBookingsAsync(int userId, int pageNumber, int pageSize, BookingStatus? status)
        {
            return await _unitOfWork.Bookings.GetPagedAsync(pageNumber, pageSize, userId, null, status);
        }

        public async Task<(IEnumerable<Booking> Items, int TotalCount)> GetTurfBookingsAsync(Guid turfId, int ownerId, int pageNumber, int pageSize)
        {
            var turf = await _unitOfWork.Turfs.GetByIdAsync(turfId);
            if (turf == null || turf.OwnerId != ownerId)
                return (Enumerable.Empty<Booking>(), 0);

            return await _unitOfWork.Bookings.GetPagedAsync(pageNumber, pageSize, null, turfId, null);
        }

        public async Task<(IEnumerable<Booking> Items, int TotalCount)> GetOwnerAllBookingsAsync(int ownerId, int pageNumber, int pageSize, BookingStatus? status)
        {
            return await _unitOfWork.Bookings.GetPagedAsync(pageNumber, pageSize, null, null, status, ownerId);
        }

        public async Task<IEnumerable<Payment>> GetPendingPaymentsForOwnerAsync(int ownerId)
        {
            return await _unitOfWork.Payments.GetPendingPaymentsForOwnerAsync(ownerId);
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────────────
        private async Task WriteAuditLogAsync(string entityType, string entityId, string action,
            string? oldValue, string? newValue, int? performedByUserId)
        {
            var log = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValue = oldValue,
                NewValue = newValue,
                PerformedByUserId = performedByUserId,
                PerformedAt = DateTime.UtcNow
            };
            await _unitOfWork.AuditLogs.AddAsync(log);
            await _unitOfWork.CompleteAsync();
        }

        private async Task SendNotificationAsync(int userId, string title, string message,
            NotificationType type, string? actionUrl)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            try
            {
                await _externalNotificationService.DispatchNotificationAsync(userId, title, message, type);
            }
            catch
            {
                // Swallow to prevent failing the core database operations if dispatch fails
            }
        }
    }
}
