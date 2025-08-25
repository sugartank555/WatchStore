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

        
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        
        public IActionResult Create()
        {
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Brand,Price,Description,Stock,CreatedAt,ImageFile")] Product product)
        {
            

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

        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

   
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
              
                product.Name = formModel.Name;
                product.Brand = formModel.Brand;
                product.Price = formModel.Price;
                product.Description = formModel.Description;
                product.Stock = formModel.Stock;
                product.CreatedAt = formModel.CreatedAt;
                if (formModel.ImageFile != null)
                {
                  
                    DeleteImageIfExists(product.ImageUrl);

                   
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

       
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        
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

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        
        private async Task<string> SaveImageAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
            {
                throw new InvalidOperationException("Định dạng ảnh không hợp lệ. Chỉ cho phép: .jpg, .jpeg, .png, .gif, .webp");
            }

          
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
            
            var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
            var fullPath = Path.Combine(imagesDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

           
            return "/images/" + fileName;
        }

     
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
