using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Models.ViewModels;

namespace WatchStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? q, string? brand)
        {
            // Brands duy nhất (cho filter + “Top brands”)
            var brands = await _db.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            // New arrivals: 8 sp mới nhất
            var newArrivals = await _db.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Best sellers: tạm thời chọn theo giá cao hoặc CreatedAt – bạn có thể thay bằng cột Sold
            var bestSellers = await _db.Products
                .OrderByDescending(p => p.Price)
                .ThenByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            var vm = new HomeVm
            {
                Brands = brands,
                NewArrivals = newArrivals,
                BestSellers = bestSellers,
                Q = q,
                Brand = brand
            };

            return View(vm);
        }

        public IActionResult Privacy() => View();
    }
}
