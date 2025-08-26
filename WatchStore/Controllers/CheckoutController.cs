using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Models;
using WatchStore.Services;

namespace WatchStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICartService _cart;
        private readonly IAdminNotifier _notifier;

        public CheckoutController(ApplicationDbContext db, ICartService cart, IAdminNotifier notifier)
        {
            _db = db; _cart = cart; _notifier = notifier;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Total = await _cart.GetTotalAsync();
            return View(new CheckoutVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutVm vm)
        {
            var items = await _cart.GetItemsAsync();
            if (!ModelState.IsValid || !items.Any())
            {
                ViewBag.Total = await _cart.GetTotalAsync();
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var order = new Order
            {
                UserId = userId,
                ReceiverName = vm.ReceiverName,
                ShippingAddress = vm.ShippingAddress,
                Phone = vm.Phone,
                TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity),
                Status = "Pending"
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var it in items)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = it.ProductId,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice
                });
            }
            await _db.SaveChangesAsync();
            await _cart.ClearAsync();

            // Gửi yêu cầu đến admin xác nhận đơn
            _ = _notifier.NotifyNewOrderAsync(order);

            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        public async Task<IActionResult> Success(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }
    }

    public class CheckoutVm
    {
        public string ReceiverName { get; set; } = default!;
        public string ShippingAddress { get; set; } = default!;
        public string Phone { get; set; } = default!;
    }
}
