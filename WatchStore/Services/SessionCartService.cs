using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Models;

namespace WatchStore.Services
{
    public class SessionCartService : ICartService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;

        private const string CART_KEY = "CART_ID";

        public SessionCartService(ApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public string GetCartId()
        {
            var ctx = _http.HttpContext!;
            if (!ctx.Session.TryGetValue(CART_KEY, out _))
            {
                ctx.Session.SetString(CART_KEY, Guid.NewGuid().ToString("N"));
            }
            return ctx.Session.GetString(CART_KEY)!;
        }

        public async Task<List<CartItem>> GetItemsAsync()
        {
            var cartId = GetCartId();
            return await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartId == cartId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task AddAsync(int productId, int quantity = 1)
        {
            var cartId = GetCartId();
            var product = await _db.Products.FindAsync(productId)
                          ?? throw new Exception("Product not found");

            var item = await _db.CartItems
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.ProductId == productId);

            if (item == null)
            {
                item = new CartItem
                {
                    CartId = cartId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };
                _db.CartItems.Add(item);
            }
            else
            {
                item.Quantity += quantity;
            }

            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(int cartItemId, int newQuantity)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item == null) return;

            if (newQuantity <= 0)
                _db.CartItems.Remove(item);
            else
                item.Quantity = newQuantity;

            await _db.SaveChangesAsync();
        }

        public async Task RemoveAsync(int cartItemId)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item == null) return;
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task ClearAsync()
        {
            var cartId = GetCartId();
            var items = _db.CartItems.Where(c => c.CartId == cartId);
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }

        public async Task<decimal> GetTotalAsync()
        {
            var cartId = GetCartId();
            return await _db.CartItems
                .Where(c => c.CartId == cartId)
                .SumAsync(c => c.UnitPrice * c.Quantity);
        }
    }
}
