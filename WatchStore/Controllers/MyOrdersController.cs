using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;

namespace WatchStore.Controllers
{
    [Authorize]
    public class MyOrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MyOrdersController(ApplicationDbContext db) => _db = db;

        // GET: /MyOrders
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderListVm
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            return View(orders);
        }

        // GET: /MyOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var order = await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            var vm = new OrderDetailsVm
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                ReceiverName = order.ReceiverName,
                ShippingAddress = order.ShippingAddress,
                Phone = order.Phone,
                Status = order.Status,
                AdminNote = order.AdminNote,
                TotalAmount = order.TotalAmount,
                Items = order.Items.Select(i => new OrderItemVm
                {
                    ProductName = i.Product?.Name ?? $"SP#{i.ProductId}",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            return View(vm);
        }
    }

    // ===== VMs =====
    public class OrderListVm
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class OrderDetailsVm
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string ReceiverName { get; set; } = default!;
        public string ShippingAddress { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Status { get; set; } = "Pending";
        public string? AdminNote { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemVm> Items { get; set; } = new();
    }

    public class OrderItemVm
    {
        public string ProductName { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
