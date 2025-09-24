using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStore.Data;
using WatchStore.Models;
using WatchStore.Models.ViewModels;

namespace WatchStore.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Dashboard/Products
        // ?q=...&brand=...&page=1&pageSize=12
        public async Task<IActionResult> Index(string? q, string? brand, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            // danh sách brand cho dropdown
            var brands = await _context.Products.AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            // base query
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = $"%{q.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Name, kw) ||
                    (p.Description != null && EF.Functions.Like(p.Description, kw)));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(p => p.Brand == brand);
            }

            var total = await query.CountAsync();

            // bảo vệ khi page vượt quá
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new AdminProductsIndexVm
            {
                Items = items,
                Brands = brands,
                Q = q,
                Brand = brand,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            return View(vm);
        }

        // GET: Dashboard/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: Dashboard/Products/Create
        public IActionResult Create() => View();

        // POST: Dashboard/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Brand,Price,Description,Stock,CreatedAt,ImageFile")] Product product)
        {
            if (product.ImageFile != null && !ValidateImage(product.ImageFile, out var error))
                ModelState.AddModelError(nameof(product.ImageFile), error);

            if (!ModelState.IsValid) return View(product);

            if (product.ImageFile != null)
                product.ImageUrl = await SaveImageAsync(product.ImageFile);

            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Dashboard/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Dashboard/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Brand,Price,Description,Stock,CreatedAt,ImageFile")] Product formModel)
        {
            if (id != formModel.Id) return NotFound();

            if (formModel.ImageFile != null && !ValidateImage(formModel.ImageFile, out var error))
                ModelState.AddModelError(nameof(formModel.ImageFile), error);

            if (!ModelState.IsValid) return View(formModel);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            product.Name = formModel.Name;
            product.Brand = formModel.Brand;
            product.Price = formModel.Price;
            product.Description = formModel.Description;
            product.Stock = formModel.Stock;
            product.CreatedAt = formModel.CreatedAt;

            if (formModel.ImageFile != null)
            {
                var newUrl = await SaveImageAsync(formModel.ImageFile);
                DeleteImageIfExists(product.ImageUrl);
                product.ImageUrl = newUrl;
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(formModel.Id)) return NotFound();
                throw;
            }
        }

        // GET: Dashboard/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Dashboard/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                DeleteImageIfExists(product.ImageUrl);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id) => _context.Products.Any(e => e.Id == id);

        // ====== Upload helpers ======
        private bool ValidateImage(IFormFile file, out string error)
        {
            error = string.Empty;
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif" }; // + AVIF
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
            {
                error = "Định dạng ảnh không hợp lệ. Chỉ cho phép: .jpg, .jpeg, .png, .gif, .webp, .avif";
                return false;
            }

            const long maxBytes = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxBytes)
            {
                error = "Ảnh vượt quá 5MB.";
                return false;
            }
            return true;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var imagesDir = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(imagesDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            return "/images/" + fileName;
        }

        private void DeleteImageIfExists(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return;
            if (!imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)) return;

            var relativePath = imageUrl.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
