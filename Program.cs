using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FluentValidation.AspNetCore;
using turf_management_system.BackgroundJobs;
using turf_management_system.Data;
using turf_management_system.Hubs;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Implementations;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;
using turf_management_system.Services;

var builder = WebApplication.CreateBuilder(args);

// ── DbContext ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repositories & UnitOfWork ─────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITurfRepository, TurfRepository>();
builder.Services.AddScoped<ITurfImageRepository, TurfImageRepository>();
builder.Services.AddScoped<ITurfSlotRepository, TurfSlotRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ISlotLockRepository, SlotLockRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITurfService, TurfService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddScoped<BookingHubNotifier>();

// ── Background Services ───────────────────────────────────────────────────────
builder.Services.AddHostedService<SlotLockCleanupService>();

// ── MVC + FluentValidation ────────────────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new turf_management_system.Helpers.TimeSpanConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// ── Authorization Policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole("SuperAdmin"));
    options.AddPolicy("PlatformAdmins", p => p.RequireRole("SuperAdmin", "Admin", "SupportAdmin", "FinanceAdmin", "OperationsAdmin"));
    options.AddPolicy("TurfManagement", p => p.RequireRole("SuperAdmin", "Admin", "TurfOwner", "TurfManager"));
    options.AddPolicy("TurfStaff", p => p.RequireRole("TurfOwner", "TurfManager", "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard"));
    options.AddPolicy("FinanceAccess", p => p.RequireRole("SuperAdmin", "FinanceAdmin", "TurfOwner", "Cashier"));
});

var app = builder.Build();

// ── Seeding ───────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    var requiredRoles = new[]
    {
        "SuperAdmin", "Admin", "SupportAdmin", "FinanceAdmin", "OperationsAdmin",
        "TurfOwner",
        "TurfManager", "Receptionist", "Groundskeeper", "Cashier", "SecurityGuard",
        "User"
    };
    var existingRoleNames = context.Roles.Select(r => r.RoleName).ToList();

    foreach (var name in requiredRoles)
    {
        if (!existingRoleNames.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            context.Roles.Add(new Role { RoleName = name, CreatedAt = DateTime.UtcNow });
        }
    }
    context.SaveChanges();

    if (!context.Users.Any(u => u.Email == "superadmin@turf.com"))
    {
        var adminRole = context.Roles.First(r => r.RoleName == "SuperAdmin");
        context.Users.Add(new User
        {
            FullName = "System Super Admin",
            Email = "superadmin@turf.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123!"),
            RoleId = adminRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    // Seed Demo Customer
    if (!context.Users.Any(u => u.Email == "customer@turf.com"))
    {
        var userRole = context.Roles.First(r => r.RoleName == "User");
        context.Users.Add(new User
        {
            FullName = "Maruf Customer",
            Email = "customer@turf.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            RoleId = userRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    // Seed Demo Turf Owner & Turf
    if (!context.Users.Any(u => u.Email == "owner@turf.com"))
    {
        var ownerRole = context.Roles.First(r => r.RoleName == "TurfOwner");
        var ownerUser = new User
        {
            FullName = "Demo Turf Owner",
            Email = "owner@turf.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            RoleId = ownerRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(ownerUser);
        context.SaveChanges();

        var turfOwner = new TurfOwner
        {
            UserId = ownerUser.UserId,
            BusinessName = "Greenfield Sports Center",
            BusinessAddress = "Sector 11, Uttara, Dhaka",
            ContactNumber = "01711223344",
            VerificationStatus = turf_management_system.Models.Enums.VerificationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };
        context.TurfOwners.Add(turfOwner);
        context.SaveChanges();

        // Seed a premium turf for this owner
        var turfId = Guid.NewGuid();
        var turf = new Turf
        {
            Id = turfId,
            Name = "Greenfield Arena",
            Description = "Premium FIFA-quality turf with international standards, great amenities, and high-quality floodlights.",
            Location = "Plot 24, Road 12, Sector 11, Uttara",
            City = "Dhaka",
            PricePerHour = 2000.00m,
            MorningPricePerHour = 1500.00m,
            EveningPricePerHour = 2500.00m,
            SportType = "Football",
            TurfSize = "7v7",
            Amenities = "Floodlights, Changing Room, Washrooms, Free WiFi, Parking, Water Station",
            IndoorOutdoor = "Outdoor",
            ContactNumber = "01711223344",
            IsApproved = true,
            IsActive = true,
            IsDraft = false,
            OwnerId = ownerUser.UserId,
            CreatedAt = DateTime.UtcNow
        };
        context.Turfs.Add(turf);

        // Seed basic booking config
        var config = new TurfBookingConfig
        {
            TurfId = turfId,
            AvailableDaysMask = 127, // All days
            OpeningTime = new TimeSpan(6, 0, 0),
            ClosingTime = new TimeSpan(22, 0, 0),
            SlotDurationMinutes = 60,
            MaxAdvanceBookingDays = 30,
            RequireFullPayment = false,
            AdvancePaymentPercent = 50.00m,
            AcceptBkash = true,
            AcceptNagad = true,
            AcceptRocket = true,
            CreatedAt = DateTime.UtcNow
        };
        context.TurfBookingConfigs.Add(config);

        // Seed slots with morning & evening pricing variants
        var slots = new List<TurfSlot>
        {
            // Morning Slots (6 AM - 12 PM)
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(7, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(8, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            
            // Afternoon Slots (12 PM - 4 PM - Morning Price fallback)
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(13, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 0, 0), PricingVariant = "Morning", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 0, 0), PricingVariant = "Morning", IsAvailable = true },

            // Evening Slots (4 PM - 10 PM - Evening Price!)
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0), PricingVariant = "Evening", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(17, 0, 0), EndTime = new TimeSpan(18, 0, 0), PricingVariant = "Evening", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0), PricingVariant = "Evening", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(20, 0, 0), PricingVariant = "Evening", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(21, 0, 0), PricingVariant = "Evening", IsAvailable = true },
            new TurfSlot { Id = Guid.NewGuid(), TurfId = turfId, StartTime = new TimeSpan(21, 0, 0), EndTime = new TimeSpan(22, 0, 0), PricingVariant = "Evening", IsAvailable = true }
        };
        context.TurfSlots.AddRange(slots);
        context.SaveChanges();
    }

}

// ── Middleware Pipeline ────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ────────────────────────────────────────────────────────────────────
app.MapControllers(); // Ensures attribute-routed API controllers are registered
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── SignalR Hub ────────────────────────────────────────────────────────────────
app.MapHub<BookingHub>("/hubs/booking");

app.Run();
