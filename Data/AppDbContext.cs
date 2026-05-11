using Microsoft.EntityFrameworkCore;
using turf_management_system.Models.Domain;

namespace turf_management_system.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Turf> Turfs { get; set; }
        public DbSet<TurfImage> TurfImages { get; set; }
        public DbSet<TurfSlot> TurfSlots { get; set; }
        public DbSet<TurfOwner> TurfOwners { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Booking entity
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalHours).HasColumnType("decimal(18,2)");

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
            });

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Turf entity
            modelBuilder.Entity<Turf>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(300);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PricePerHour).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SportType).IsRequired().HasMaxLength(100);

                entity.HasOne(d => d.Owner)
                    .WithMany()
                    .HasForeignKey(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Soft delete filter
                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            // Configure TurfImage entity
            modelBuilder.Entity<TurfImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.Turf)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TurfSlot entity
            modelBuilder.Entity<TurfSlot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.Turf)
                    .WithMany(p => p.Slots)
                    .HasForeignKey(d => d.TurfId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TurfOwner entity
            modelBuilder.Entity<TurfOwner>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.BusinessName).IsRequired().HasMaxLength(200);
                entity.HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<TurfOwner>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
