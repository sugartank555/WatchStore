using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Services;     // Cart service

var builder = WebApplication.CreateBuilder(args);

// ========== DB ==========
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ========== Identity (IdentityUser) + Roles ==========
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // cho demo
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ========== MVC ==========
builder.Services.AddControllersWithViews();

// ========== Session cho giỏ hàng ==========
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ========== DI cho Cart ==========
builder.Services.AddScoped<ICartService, SessionCartService>();

var app = builder.Build();

// ========== Pipeline ==========
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
           name: "Dashborad",
           pattern: "{area:exists}/{controller=Products}/{action=Index}/{id?}"
         );
// Route mặc định: Home/Index (giữ nguyên theo yêu cầu)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Razor Pages cho Identity (Areas/Identity)
app.MapRazorPages();

// ========== Seed dữ liệu (nếu có) ==========
// Nếu bạn có SeedData dùng ApplicationUser, hãy sửa sang IdentityUser hoặc tạm thời comment lại dòng dưới.
using (var scope = app.Services.CreateScope())
 {
     await SeedData.InitializeAsync(scope.ServiceProvider);
 }

app.Run();
