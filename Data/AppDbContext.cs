using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using turf_management_system.Models.Domain;

namespace turf_management_system.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Core
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<TurfOwner> TurfOwners { get; set; }
        public DbSet<StaffProfile> StaffProfiles { get; set; }

        // Turf
        public DbSet<Turf> Turfs { get; set; }
        public DbSet<TurfImage> TurfImages { get; set; }
        public DbSet<TurfSlot> TurfSlots { get; set; }
        public DbSet<TurfBookingConfig> TurfBookingConfigs { get; set; }



        // Booking & Payment
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<SlotLock> SlotLocks { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // System
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── PasswordResetToken ──────────────────────────────────────
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(64);
                entity.HasIndex(e => e.TokenHash);
                entity.HasIndex(e => e.UserId);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Role ────────────────────────────────────────────────────
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.RoleName).IsUnique();
            });

            // ─── User ────────────────────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── TurfOwner ───────────────────────────────────────────────
            modelBuilder.Entity<TurfOwner>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.BusinessName).IsRequired().HasMaxLength(200);
                entity.HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<TurfOwner>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── StaffProfile ────────────────────────────────────────────
            modelBuilder.Entity<StaffProfile>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<StaffProfile>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(d => d.Turf)
                    .WithMany()
                    .HasForeignKey(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Turf ────────────────────────────────────────────────────
            modelBuilder.Entity<Turf>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(300);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PricePerHour).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MorningPricePerHour).HasColumnType("decimal(18,2)");
                entity.Property(e => e.EveningPricePerHour).HasColumnType("decimal(18,2)");

                entity.Property(e => e.SportType).IsRequired().HasMaxLength(100);

                entity.HasOne(d => d.Owner)
                    .WithMany()
                    .HasForeignKey(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            // ─── TurfImage ───────────────────────────────────────────────
            modelBuilder.Entity<TurfImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.Turf)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── TurfSlot ────────────────────────────────────────────────
            modelBuilder.Entity<TurfSlot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.Turf)
                    .WithMany(p => p.Slots)
                    .HasForeignKey(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── TurfBookingConfig ────────────────────────────────────────
            modelBuilder.Entity<TurfBookingConfig>(entity =>
            {
                entity.HasKey(e => e.TurfId);
                entity.Property(e => e.AdvancePaymentPercent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.RefundPercent).HasColumnType("decimal(5,2)");

                entity.HasOne(d => d.Turf)
                    .WithOne()
                    .HasForeignKey<TurfBookingConfig>(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Booking ─────────────────────────────────────────────────
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalHours).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Turf)
                    .WithMany()
                    .HasForeignKey(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Slot)
                    .WithMany()
                    .HasForeignKey(d => d.SlotId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(d => d.Payments)
                    .WithOne(p => p.Booking)
                    .HasForeignKey(p => p.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── SlotLock ─────────────────────────────────────────────────
            modelBuilder.Entity<SlotLock>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Composite index for fast overlap queries
                entity.HasIndex(e => new { e.TurfId, e.BookingDate, e.StartTime, e.EndTime })
                    .HasDatabaseName("IX_SlotLock_TurfDateRange");

                entity.HasOne(d => d.Turf)
                    .WithMany()
                    .HasForeignKey(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.LockedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.LockedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Optional FK to Booking - no cascade since booking may not exist yet
                entity.HasOne(d => d.Booking)
                    .WithMany()
                    .HasForeignKey(d => d.BookingId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── Payment ──────────────────────────────────────────────────
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

                // Allow same transaction ID for multiple bookings in a single checkout transaction
                entity.HasIndex(e => e.TransactionId)
                    .HasDatabaseName("IX_Payment_TransactionId");

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // VerifiedByAdmin self-referencing FK - no cascade
                entity.HasOne(d => d.VerifiedByAdmin)
                    .WithMany()
                    .HasForeignKey(d => d.VerifiedByAdminId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── AuditLog ────────────────────────────────────────────────
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.PerformedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.PerformedByUserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── Notification ────────────────────────────────────────────
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
                v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }
    }
}
