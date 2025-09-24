using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Services;

namespace WatchStore.Controllers
{
    [Authorize] // bắt buộc đăng nhập để gửi yêu cầu thanh toán/xác nhận
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAdminNotifier _notifier;

        public PaymentsController(ApplicationDbContext db, IAdminNotifier notifier)
        {
            _db = db;
            _notifier = notifier;
        }

        /// <summary>
        /// Gửi yêu cầu xác nhận thanh toán/đơn hàng lên Admin (không dùng cổng thanh toán).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();

            // Nếu đơn chưa ở trạng thái chờ duyệt, chuyển về Pending
            // (Bạn tùy chỉnh thêm các trạng thái: Draft/Created/... nếu có)
            if (string.IsNullOrWhiteSpace(order.Status) || order.Status == "Draft" || order.Status == "Created")
            {
                order.Status = "Pending";
                await _db.SaveChangesAsync();
            }

            // Gửi mail/thông báo cho admin biết có đơn chờ xác nhận
            try
            {
                await _notifier.NotifyNewOrderAsync(order);
            }
            catch
            {
                // nuốt lỗi thông báo để không làm fail UX người dùng
            }

            // Điều hướng người dùng về trang chi tiết đơn của họ (hoặc trang "MyOrders")
            return RedirectToAction("Details", "MyOrders", new { id = orderId });
        }

        /// <summary>
        /// (Tuỳ chọn) Trang hướng dẫn thanh toán chuyển khoản thủ công.
        /// Không QR/cổng thanh toán – chỉ hiển thị thông tin tài khoản.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Instructions(int orderId)
        {
            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();

            // Có thể ViewBag.BankName/AccountNumber từ cấu hình appsettings để show lên View.
            ViewBag.BankName = "Vietcombank";
            ViewBag.AccountName = "CONG TY WATCHSTORE";
            ViewBag.AccountNumber = "0123456789";
            ViewBag.TransferNote = $"ORDER{order.Id} {User.Identity?.Name}";

            return View(order);
        }
    }
}
