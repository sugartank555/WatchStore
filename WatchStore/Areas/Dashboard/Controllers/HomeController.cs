using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;

namespace WatchStore.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null)
        {
            // Khung thời gian mặc định: 6 tháng gần đây (tính đến hôm nay, timezone VN)
            var now = DateTime.UtcNow.AddHours(7);
            var end = (to ?? now).Date.AddDays(1).AddTicks(-1);
            var start = from?.Date ?? now.AddMonths(-5).Date.AddDays(1 - now.Day); // đầu tháng cách đây 5 tháng

            // KPI nhanh
            var totalOrders = await _db.Orders.AsNoTracking().CountAsync();
            var pendingOrders = await _db.Orders.AsNoTracking().CountAsync(o => o.Status == "Pending");
            var confirmedOrders = await _db.Orders.AsNoTracking().CountAsync(o => o.Status == "Confirmed" || o.Status == "Paid");
            var totalRevenue = await _db.Orders.AsNoTracking()
                .Where(o => (o.Status == "Paid" || o.Status == "Confirmed") && o.OrderDate >= start && o.OrderDate <= end)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.ConfirmedOrders = confirmedOrders;
            ViewBag.TotalRevenue = totalRevenue;

            // Biểu đồ doanh thu 6 tháng (theo khung thời gian)
            var months = new List<(int y, int m)>();
            var cursor = new DateTime(start.Year, start.Month, 1);
            var endMonth = new DateTime(end.Year, end.Month, 1);
            while (cursor <= endMonth)
            {
                months.Add((cursor.Year, cursor.Month));
                cursor = cursor.AddMonths(1);
            }

            var revQuery = await _db.Orders.AsNoTracking()
                .Where(o => (o.Status == "Paid" || o.Status == "Confirmed") && o.OrderDate >= start && o.OrderDate <= end)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var labels = months.Select(mm => new DateTime(mm.y, mm.m, 1).ToString("MM/yyyy")).ToList();
            var series = months.Select(mm =>
                (decimal)(revQuery.FirstOrDefault(x => x.Year == mm.y && x.Month == mm.m)?.Sum ?? 0m)
            ).ToList();

            ViewBag.ChartLabels = labels;          // List<string>
            ViewBag.ChartRevenue = series;         // List<decimal>

            // Doughnut trạng thái trong khung thời gian
            var statusGroups = await _db.Orders.AsNoTracking()
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusLabels = new[] { "Pending", "Confirmed", "Paid", "Rejected", "Cancelled" };
            var statusData = statusLabels.Select(s => statusGroups.FirstOrDefault(x => x.Status == s)?.Count ?? 0).ToList();
            ViewBag.StatusLabels = statusLabels;
            ViewBag.StatusData = statusData;

            // Đơn gần đây
            var recentOrders = await _db.Orders.AsNoTracking()
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();
            ViewBag.RecentOrders = recentOrders;

            // Trả về để bind lại input filter
            ViewBag.From = start.Date;
            ViewBag.To = end.Date;

            return View();
        }
    }
}
