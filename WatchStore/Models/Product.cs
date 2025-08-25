using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <- để dùng [NotMapped]
using Microsoft.AspNetCore.Http;                  // <- để dùng IFormFile

namespace WatchStore.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = default!;

        [StringLength(200)]
        public string? Brand { get; set; }

        [Range(0, 100000000)]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Lưu đường dẫn ảnh sau khi upload (ví dụ: /images/tenfile.jpg)
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int Stock { get; set; } = 100;

        // Nên lưu UTC để nhất quán
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Dùng cho binding file upload, không lưu vào DB
        [NotMapped]
        [Display(Name = "Image")]
        public IFormFile? ImageFile { get; set; }
    }
}
