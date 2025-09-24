using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Models.ViewModels;

namespace WatchStore.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) => _db = db;

        // GET: Dashboard/Orders
        // ?q=...&status=...&page=1&pageSize=12
        public async Task<IActionResult> Index(string? q, string? status, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            var query = _db.Orders
                .Include(o => o.User)
                .AsNoTracking()
                .AsQueryable();

            // tìm kiếm theo tên user hoặc mã đơn
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = $"%{q.Trim()}%";
                query = query.Where(o =>
                    EF.Functions.Like(o.Id.ToString(), kw) ||
                    (o.User != null && EF.Functions.Like(o.User.UserName!, kw)) ||
                    (o.User != null && o.User.Email != null && EF.Functions.Like(o.User.Email!, kw)));
            }

            // lọc trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new AdminOrdersIndexVm
            {
                Items = items,
                Q = q,
                Status = status,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            return View(vm); // Areas/Dashboard/Views/Orders/Index.cshtml
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
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
