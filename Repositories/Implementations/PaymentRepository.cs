using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context) { }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            return await _dbSet
                .Include(p => p.Booking)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByBookingIdAsync(Guid bookingId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPendingPaymentsForOwnerAsync(int ownerId)
        {
            return await _dbSet
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Turf)
                .Include(p => p.User)
                .Where(p => p.Status == PaymentVerificationStatus.Pending
                         && p.Booking.Turf.OwnerId == ownerId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();
        }

        public async Task<bool> TransactionIdExistsAsync(string transactionId)
        {
            return await _dbSet.AnyAsync(p => p.TransactionId == transactionId);
        }
    }
}
