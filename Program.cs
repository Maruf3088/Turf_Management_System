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
    
    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Role { RoleName = "Admin", CreatedAt = DateTime.UtcNow },
            new Role { RoleName = "TurfOwner", CreatedAt = DateTime.UtcNow },
            new Role { RoleName = "User", CreatedAt = DateTime.UtcNow }
        );
        context.SaveChanges();
    }
    else
    {
        // Ensure TurfOwner exists and Role names are correct for IDs
        var roles = context.Roles.ToList();
        var adminRole = roles.FirstOrDefault(r => r.RoleId == 1);
        var turfOwnerRole = roles.FirstOrDefault(r => r.RoleId == 2);
        var userRole = roles.FirstOrDefault(r => r.RoleId == 3);

        if (adminRole != null && adminRole.RoleName != "Admin") { adminRole.RoleName = "Admin"; }
        
        if (turfOwnerRole == null)
        {
            // This might happen if we had 2 roles before.
            // If RoleId 2 was "User", we need to change it to "TurfOwner" and add "User" as 3.
            var role2 = context.Roles.Find(2);
            if (role2 != null && role2.RoleName == "User")
            {
                role2.RoleName = "TurfOwner";
                context.Roles.Add(new Role { RoleName = "User", CreatedAt = DateTime.UtcNow });
            }
            else
            {
                context.Roles.Add(new Role { RoleName = "TurfOwner", CreatedAt = DateTime.UtcNow });
            }
        }
        else if (turfOwnerRole.RoleName == "User")
        {
            // Shift User from 2 to 3
            turfOwnerRole.RoleName = "TurfOwner";
            if (userRole == null)
            {
                context.Roles.Add(new Role { RoleName = "User", CreatedAt = DateTime.UtcNow });
            }
        }
        
        context.SaveChanges();
    }

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
