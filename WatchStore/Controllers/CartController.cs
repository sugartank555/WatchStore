using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Services;

namespace WatchStore.Controllers
{
  
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        public CartController(ICartService cart) => _cart = cart;

        // GET: /Cart/Add
        [HttpGet]
        public IActionResult Add(int productId = 0)
        {
            ViewBag.ProductId = productId; // nếu được truyền sẵn từ link
            return View();
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;
            await _cart.AddAsync(productId, quantity);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Total = await _cart.GetTotalAsync();
            return View(await _cart.GetItemsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            await _cart.UpdateAsync(cartItemId, quantity);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            await _cart.RemoveAsync(cartItemId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            await _cart.ClearAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
