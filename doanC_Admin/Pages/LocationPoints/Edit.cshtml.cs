using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using doanC_Admin.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace doanC_Admin.Pages.LocationPoints
{
    public class EditModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EditModel(FoodStreetGuideDBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public LocationPoint LocationPoint { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            LocationPoint = await _context.LocationPoints.FindAsync(id);

            if (LocationPoint == null)
            {
                return NotFound();
            }

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

            var existingLocation = await _context.LocationPoints.FindAsync(LocationPoint.PointId);

            if (existingLocation == null)
            {
                return NotFound();
            }

            // Xử lý upload ảnh mới
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(existingLocation.Image))
                {
                    DeleteOldImage(existingLocation.Image);
                }

                // Lưu ảnh mới
                var fileName = await SaveImageAsync(ImageFile);
                existingLocation.Image = fileName;
            }

            // Cập nhật các trường
            existingLocation.Name = LocationPoint.Name;
            existingLocation.Description = LocationPoint.Description;
            existingLocation.Latitude = LocationPoint.Latitude;
            existingLocation.Longitude = LocationPoint.Longitude;
            existingLocation.Address = LocationPoint.Address;
            existingLocation.Category = LocationPoint.Category;
            existingLocation.Rating = LocationPoint.Rating;
            existingLocation.ReviewCount = LocationPoint.ReviewCount;
            existingLocation.OpeningHours = LocationPoint.OpeningHours;
            existingLocation.PriceRange = LocationPoint.PriceRange;
            existingLocation.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Lấy đường dẫn đến thư mục Images của MAUI app (ĐƯỜNG DẪN TƯƠNG ĐỐI)
        /// </summary>
        private string GetMauiImagesPath()
        {
            // Lấy đường dẫn thư mục doanC_Admin
            var adminDirectory = _webHostEnvironment.ContentRootPath;

            // Lên 1 cấp để đến thư mục FoodStreetGuide_Solution
            var solutionDirectory = Path.GetDirectoryName(adminDirectory);

            // Đường dẫn đến thư mục Images của MAUI app
            var imagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "Images");

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
                Console.WriteLine($"[EditModel] Created directory: {imagesPath}");
            }

            Console.WriteLine($"[EditModel] Images path: {imagesPath}");
            return imagesPath;
        }

        /// <summary>
        /// Lưu file ảnh vào thư mục Images của MAUI app
        /// </summary>
        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            // Lấy phần mở rộng file
            var extension = Path.GetExtension(imageFile.FileName).ToLower();

            // Tạo tên file hợp lệ
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 12);
            var fileName = $"loc_{timestamp}_{uniqueId}{extension}";

            var imagesPath = GetLocationImagesPath();
            var filePath = Path.Combine(imagesPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }
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

        /// <summary>
        /// Xóa file ảnh cũ
        /// </summary>
        private void DeleteOldImage(string imageName)
        {
            try
            {
                var imagesPath = GetMauiImagesPath();
                var filePath = Path.Combine(imagesPath, imageName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"[EditModel] 🗑️ Deleted old image: {imageName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EditModel] Error deleting image: {ex.Message}");
            }
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
                new SelectListItem { Value = "Quán ăn", Text = "🍜 Quán ăn" },
                new SelectListItem { Value = "Cafe", Text = "☕ Cafe" },
                new SelectListItem { Value = "Bar - Pub", Text = "🍺 Bar - Pub" },
                new SelectListItem { Value = "Chùa - Miếu", Text = "🛕 Chùa - Miếu" },
                new SelectListItem { Value = "Bảo tàng", Text = "🏛️ Bảo tàng" }
            };
        }
    }
}