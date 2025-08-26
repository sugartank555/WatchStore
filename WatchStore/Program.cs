using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Services;     

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddScoped<ICartService, SessionCartService>();
builder.Services.AddScoped<IEmailSenderSimple, SmtpEmailSender>();
builder.Services.AddScoped<IAdminNotifier, AdminEmailNotifier>();

var app = builder.Build();

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

app.MapControllerRoute(
    name: "Products",
    pattern: "{controller=Products}/{action=Index}/{id?}");


app.MapRazorPages();

using (var scope = app.Services.CreateScope())
 {
     await SeedData.InitializeAsync(scope.ServiceProvider);
 }

app.Run();
