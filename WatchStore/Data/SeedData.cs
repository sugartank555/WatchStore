using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WatchStore.Models; // để dùng Product

namespace WatchStore.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await db.Database.MigrateAsync();

            // Roles
            if (!await roleMgr.RoleExistsAsync("Admin"))
                await roleMgr.CreateAsync(new IdentityRole("Admin"));

            // Admin user
            var adminEmail = "admin@watchstore.local";
            var admin = await userMgr.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userMgr.CreateAsync(admin, "Admin@123");
                await userMgr.AddToRoleAsync(admin, "Admin");
            }

            // Seed Products
            if (!await db.Products.AnyAsync())
            {
                db.Products.AddRange(
                    new Product { Name = "Omega Seamaster", Brand = "Omega", Price = 250000000, ImageUrl = "/images/omega.jpg", Description = "Diver 300M Co-Axial Master Chronometer 42 mm" },
                    new Product { Name = "Rolex Submariner", Brand = "Rolex", Price = 320000000, ImageUrl = "/images/rolex.jpg", Description = "Oystersteel 41mm" },
                    new Product { Name = "Casio G-Shock GA-2100", Brand = "Casio", Price = 3500000, ImageUrl = "/images/casio.jpg", Description = "Carbon Core Guard" }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
