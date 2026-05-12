using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IGenericRepository<Role> Roles { get; }
        ITurfRepository Turfs { get; }
        ITurfImageRepository TurfImages { get; }
        ITurfSlotRepository TurfSlots { get; }
        IGenericRepository<TurfOwner> TurfOwners { get; }
        IGenericRepository<StaffProfile> StaffProfiles { get; }
        IBookingRepository Bookings { get; }
        IPaymentRepository Payments { get; }
        ISlotLockRepository SlotLocks { get; }
        IGenericRepository<TurfBookingConfig> BookingConfigs { get; }
        IGenericRepository<AuditLog> AuditLogs { get; }
        IGenericRepository<Notification> Notifications { get; }
        Task<int> CompleteAsync();
    }
}
