using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using doanC_Admin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using doanC_Admin.Helpers;

namespace doanC_Admin.Pages.LocationPoints
{
    public class CreateModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public CreateModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LocationPoint LocationPoint { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            LoadCategories();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            // Xử lý upload ảnh
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = await SaveImageAsync(ImageFile);
                LocationPoint.Image = fileName;
            }

            LocationPoint.CreatedAt = DateTime.Now;
            LocationPoint.UpdatedAt = DateTime.Now;

            _context.LocationPoints.Add(LocationPoint);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        // 👈 CHỈ GIỮ 1 METHOD SaveImageAsync DUY NHẤT
        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            // Lấy phần mở rộng file (ví dụ: .jpg, .png)
            var extension = Path.GetExtension(imageFile.FileName).ToLower();

            // Tạo tên file hợp lệ: bắt đầu bằng chữ, chỉ gồm chữ và số
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 12); // Lấy 12 ký tự đầu
            var fileName = $"loc_{timestamp}_{uniqueId}{extension}";

            var imagesPath = GetLocationImagesPath();
            var filePath = Path.Combine(imagesPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }
        // Helper lấy đường dẫn
        private string GetLocationImagesPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
            var imagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "Images");

            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }
            return imagesPath;
        }


        private void LoadCategories()
        {
            Categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "Ăn vặt", Text = "🍢 Ăn vặt" },
                new SelectListItem { Value = "Đồ uống", Text = "☕ Đồ uống" },
                new SelectListItem { Value = "Đồ nướng", Text = "🔥 Đồ nướng" },
                new SelectListItem { Value = "Hải sản", Text = "🦐 Hải sản" },
                new SelectListItem { Value = "Chợ - Ẩm thực", Text = "🛒 Chợ - Ẩm thực" },
                new SelectListItem { Value = "Đi bộ", Text = "🚶 Đi bộ" },
                new SelectListItem { Value = "Di tích", Text = "🏛️ Di tích" },
                new SelectListItem { Value = "Thiên nhiên", Text = "🌿 Thiên nhiên" },
                new SelectListItem { Value = "Điểm cao", Text = "🗼 Điểm cao" },
                new SelectListItem { Value = "Nhà hàng", Text = "🍽️ Nhà hàng" },
                new SelectListItem { Value = "Quán ăn", Text = "🍜 Quán ăn" }
            };
        }
    }
}