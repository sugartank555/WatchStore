using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;

namespace WatchStore.Areas.Admin.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var totalOrders = await _db.Orders.CountAsync();
            var pending = await _db.Orders.CountAsync(o => o.Status == "Pending");
            var confirmed = await _db.Orders.CountAsync(o => o.Status == "Confirmed");
            var revenue = await _db.Orders.Where(o => o.Status == "Confirmed").SumAsync(o => o.TotalAmount);

            ViewBag.TotalOrders = totalOrders;
            ViewBag.Pending = pending;
            ViewBag.Confirmed = confirmed;
            ViewBag.Revenue = revenue;
            var latest = await _db.Orders.OrderByDescending(o => o.OrderDate).Take(10).ToListAsync();
            return View(latest);
        }
    }
}
