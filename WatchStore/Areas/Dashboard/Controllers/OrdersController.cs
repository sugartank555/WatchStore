using System.Linq; // cần cho Where/OrderBy
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;

namespace WatchStore.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var orders = await _db.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders); // -> Areas/Dashboard/Views/Orders/Index.cshtml
        }

        public async Task<IActionResult> Pending()
        {
            var orders = await _db.Orders
                .Where(o => o.Status == "Pending")
                .OrderBy(o => o.OrderDate)
                .ToListAsync();
            return View(orders); // -> Areas/Dashboard/Views/Orders/Pending.cshtml
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order); // -> Areas/Dashboard/Views/Orders/Details.cshtml
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id, string? note)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            order.Status = "Confirmed";
            order.AdminNote = note;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? note)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            order.Status = "Rejected";
            order.AdminNote = note;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
