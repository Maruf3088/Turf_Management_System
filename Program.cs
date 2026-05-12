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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── SignalR Hub ────────────────────────────────────────────────────────────────
app.MapHub<BookingHub>("/hubs/booking");

app.Run();
