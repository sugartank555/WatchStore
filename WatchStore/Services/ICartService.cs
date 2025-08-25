using WatchStore.Models;

namespace WatchStore.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetItemsAsync();
        Task AddAsync(int productId, int quantity = 1);
        Task UpdateAsync(int cartItemId, int newQuantity);
        Task RemoveAsync(int cartItemId);
        Task ClearAsync();
        Task<decimal> GetTotalAsync();
        string GetCartId();
    }
}
