using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<Payment>> GetPaymentsByBookingIdAsync(Guid bookingId);
        Task<IEnumerable<Payment>> GetPendingPaymentsForOwnerAsync(int ownerId);
        Task<bool> TransactionIdExistsAsync(string transactionId);
    }
}
