using WatchStore.Models;
namespace WatchStore.Services
{
    public interface IAdminNotifier
    {
        Task NotifyNewOrderAsync(Order order);
    }
}
