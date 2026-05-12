using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IUserRepository Users { get; private set; }
        public IGenericRepository<Role> Roles { get; private set; }
        public ITurfRepository Turfs { get; private set; }
        public ITurfImageRepository TurfImages { get; private set; }
        public ITurfSlotRepository TurfSlots { get; private set; }
        public IGenericRepository<TurfOwner> TurfOwners { get; private set; }
        public IGenericRepository<StaffProfile> StaffProfiles { get; private set; }
        public IBookingRepository Bookings { get; private set; }
        public IPaymentRepository Payments { get; private set; }
        public ISlotLockRepository SlotLocks { get; private set; }
        public IGenericRepository<TurfBookingConfig> BookingConfigs { get; private set; }
        public IGenericRepository<AuditLog> AuditLogs { get; private set; }
        public IGenericRepository<Notification> Notifications { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new UserRepository(_context);
            Roles = new GenericRepository<Role>(_context);
            Turfs = new TurfRepository(_context);
            TurfImages = new TurfImageRepository(_context);
            TurfSlots = new TurfSlotRepository(_context);
            TurfOwners = new GenericRepository<TurfOwner>(_context);
            StaffProfiles = new GenericRepository<StaffProfile>(_context);
            Bookings = new BookingRepository(_context);
            Payments = new PaymentRepository(_context);
            SlotLocks = new SlotLockRepository(_context);
            BookingConfigs = new GenericRepository<TurfBookingConfig>(_context);
            AuditLogs = new GenericRepository<AuditLog>(_context);
            Notifications = new GenericRepository<Notification>(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
