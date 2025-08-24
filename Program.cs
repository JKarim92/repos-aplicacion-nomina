using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<NominaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NominaDb")));

// Cookie Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RRHHOrAdmin", policy => policy.RequireRole("RRHH", "Admin"));
});

builder.Services.AddControllersWithViews();

// Domain services
builder.Services.AddScoped<SalaryService>();
builder.Services.AddScoped<DeptEmpService>();
builder.Services.AddScoped<DeptManagerService>();
builder.Services.AddScoped<UserSeed>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SalaryService>();

var app = builder.Build();

// Apply migrations and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NominaDbContext>();
    db.Database.Migrate();

    var seeder = scope.ServiceProvider.GetRequiredService<UserSeed>();
    await seeder.SeedAsync();
}

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
