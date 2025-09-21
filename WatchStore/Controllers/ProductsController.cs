using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Models;
using WatchStore.Models.ViewModels;

namespace WatchStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        // Hỗ trợ tìm kiếm theo tên/mô tả và lọc theo brand
        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? brand)
        {
            // Lấy danh sách brand duy nhất
            var brands = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            // Query sản phẩm
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p =>
                    p.Name.Contains(q) ||
                    (p.Description != null && p.Description.Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(p => p.Brand == brand);
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var vm = new ProductIndexVm
            {
                Products = products,
                Brands = brands,
                Q = q,
                Brand = brand
            };

            return View(vm);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
