using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Implementations;
using turf_management_system.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories & UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// Seed Admin User
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    
    // Idempotent seeding: ensure required role names exist. Do not rely on fixed RoleId values
    var requiredRoles = new[] { "Admin", "TurfOwner", "User" };
    var existingRoleNames = context.Roles.Select(r => r.RoleName).ToList();

    foreach (var name in requiredRoles)
    {
        if (!existingRoleNames.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            context.Roles.Add(new Role { RoleName = name, CreatedAt = DateTime.UtcNow });
        }
    }

    context.SaveChanges();

    if (!context.Users.Any(u => u.Email == "admin@turf.com"))
    {
        var adminRole = context.Roles.First(r => r.RoleName == "Admin");
        context.Users.Add(new User
        {
            FullName = "System Admin",
            Email = "admin@turf.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
            RoleId = adminRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
