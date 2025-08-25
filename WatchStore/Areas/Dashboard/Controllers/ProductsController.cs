using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WatchStore.Data;
using WatchStore.Models;

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
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.AsNoTracking().ToListAsync());
        }

        // GET: Dashboard/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Dashboard/Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Dashboard/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Brand,Price,Description,Stock,CreatedAt,ImageFile")] Product product)
        {
            // Nếu bạn muốn bắt buộc có ảnh, bỏ comment 2 dòng dưới:
            // if (product.ImageFile == null) ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh.");
            // if (!ModelState.IsValid) return View(product);

            if (ModelState.IsValid)
            {
                if (product.ImageFile != null)
                {
                    product.ImageUrl = await SaveImageAsync(product.ImageFile);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
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

            if (!ModelState.IsValid) return View(formModel);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            try
            {
                // Cập nhật các trường cho phép chỉnh sửa
                product.Name = formModel.Name;
                product.Brand = formModel.Brand;
                product.Price = formModel.Price;
                product.Description = formModel.Description;
                product.Stock = formModel.Stock;
                product.CreatedAt = formModel.CreatedAt; // hoặc giữ nguyên nếu không muốn cho sửa thời gian tạo

                if (formModel.ImageFile != null)
                {
                    // Xóa ảnh cũ (nếu có)
                    DeleteImageIfExists(product.ImageUrl);

                    // Lưu ảnh mới
                    product.ImageUrl = await SaveImageAsync(formModel.ImageFile);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(formModel.Id))
                    return NotFound();
                throw;
            }
        }

        // GET: Dashboard/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
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
                // Xóa file ảnh trên đĩa (nếu có)
                DeleteImageIfExists(product.ImageUrl);

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        /// <summary>
        /// Lưu ảnh vào wwwroot/images và trả về đường dẫn tương đối (ví dụ "/images/abc_20250101120000.jpg").
        /// Có kiểm tra phần mở rộng và tạo thư mục nếu chưa có.
        /// </summary>
        private async Task<string> SaveImageAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            // Kiểm tra loại file cơ bản
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
            {
                throw new InvalidOperationException("Định dạng ảnh không hợp lệ. Chỉ cho phép: .jpg, .jpeg, .png, .gif, .webp");
            }

            // (Tuỳ chọn) giới hạn kích thước, ví dụ 5MB
            const long maxBytes = 5 * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                throw new InvalidOperationException("Ảnh vượt quá 5MB.");
            }

            var imagesDir = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(imagesDir))
            {
                Directory.CreateDirectory(imagesDir);
            }

            var safeName = Path.GetFileNameWithoutExtension(file.FileName);
            // Tạo tên file duy nhất
            var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
            var fullPath = Path.Combine(imagesDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về đường dẫn tương đối để lưu DB
            return "/images/" + fileName;
        }

        /// <summary>
        /// Xóa file ảnh vật lý nếu tồn tại
        /// </summary>
        private void DeleteImageIfExists(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return;

            var relativePath = imageUrl.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
