using System.Text;
using WatchStore.Models;

namespace WatchStore.Services
{
    public class AdminEmailNotifier : IAdminNotifier
    {
        private readonly IConfiguration _cfg;
        private readonly IEmailSenderSimple _mail;

        public AdminEmailNotifier(IConfiguration cfg, IEmailSenderSimple mail)
        {
            _cfg = cfg; _mail = mail;
        }

        public async Task NotifyNewOrderAsync(Order order)
        {
            var adminEmail = _cfg["Admin:Email"] ?? throw new InvalidOperationException("Admin email missing");
            var sb = new StringBuilder();
            sb.Append($"<h3>Đơn hàng mới #{order.Id}</h3>");
            sb.Append($"<p>Ngày: {order.OrderDate.ToLocalTime()}</p>");
            sb.Append($"<p>Khách: {order.ReceiverName} — {order.Phone}</p>");
            sb.Append($"<p>Địa chỉ: {order.ShippingAddress}</p>");
            sb.Append($"<p>Tổng tiền: <b>{order.TotalAmount:#,0} ₫</b></p>");
            sb.Append("<p><a href=\"https://localhost:7028/Admin/Orders/Details/" + order.Id + "\">Mở trong Dashboard</a></p>");

            await _mail.SendAsync(adminEmail, $"[WatchStore] Đơn mới #{order.Id}", sb.ToString());
        }
    }
}
