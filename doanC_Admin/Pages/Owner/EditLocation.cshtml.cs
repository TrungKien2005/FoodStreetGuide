using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using doanC_Admin.Helpers;

namespace doanC_Admin.Pages.Owner
{
    [doanC_Admin.Helpers.Authorize("Owner", "Manager")]
    public class EditLocationModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditLocationModel(FoodStreetGuideDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public LocationPoint LocationPoint { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            var location = await _context.LocationPoints.FindAsync(id);
            if (location == null || location.OwnerId != storeOwner.OwnerId)
            {
                return RedirectToPage("/Owner/MyLocations");
            }

            LocationPoint = location;
            LoadCategories();
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            // Validation
            if (string.IsNullOrWhiteSpace(LocationPoint.Name))
            {
                ErrorMessage = "Tên địa điểm không được để trống";
                LoadCategories();
                return Page();
            }

            if (LocationPoint.Latitude == 0 || LocationPoint.Longitude == 0)
            {
                ErrorMessage = "Vui lòng nhập tọa độ hợp lệ (kinh độ, vĩ độ)";
                LoadCategories();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(LocationPoint.Category))
            {
                ErrorMessage = "Vui lòng chọn danh mục";
                LoadCategories();
                return Page();
            }

            var existingLocation = await _context.LocationPoints.FindAsync(LocationPoint.PointId);
            if (existingLocation == null || existingLocation.OwnerId != storeOwner.OwnerId)
            {
                return RedirectToPage("/Owner/MyLocations");
            }

            // Cập nhật thông tin
            existingLocation.Name = LocationPoint.Name;
            existingLocation.Category = LocationPoint.Category;
            existingLocation.Description = LocationPoint.Description;
            existingLocation.Address = LocationPoint.Address;
            existingLocation.Latitude = LocationPoint.Latitude;
            existingLocation.Longitude = LocationPoint.Longitude;
            existingLocation.OpeningHours = LocationPoint.OpeningHours;
            existingLocation.PriceRange = LocationPoint.PriceRange;
            existingLocation.Phone = LocationPoint.Phone;  // ✅ Cập nhật Phone

            // ✅ QUAN TRỌNG: Đặt lại IsApproved = false để Admin duyệt lại
            existingLocation.IsApproved = false;
            existingLocation.UpdatedAt = DateTime.Now;

            // Xử lý ảnh - Xóa ảnh cũ nếu có ảnh mới
            if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingLocation.Image))
                {
                    var oldImagePath = Path.Combine(GetImagesPath(), existingLocation.Image);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var fileName = $"{Guid.NewGuid()}_{ImageFile.FileName}";
                var filePath = Path.Combine(GetImagesPath(), fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                existingLocation.Image = fileName;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thành công! Địa điểm đã được gửi lại cho Admin chờ phê duyệt.";
            return RedirectToPage("/Owner/MyLocations");
        }

        private string GetImagesPath()
        {
            var imagesPath = Path.Combine(_environment.WebRootPath, "images", "locations");
            if (!Directory.Exists(imagesPath))
                Directory.CreateDirectory(imagesPath);
            return imagesPath;
        }

        private void LoadCategories()
        {
            // ✅ Đồng bộ với AppResources.cs (dùng UTF-8 trực tiếp)
            Categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "Ăn vặt",        Text = "🍢 Ăn vặt" },
                new SelectListItem { Value = "Đồ uống",       Text = "☕ Đồ uống" },
                new SelectListItem { Value = "Đồ nướng",      Text = "🔥 Đồ nướng" },
                new SelectListItem { Value = "Hải sản",       Text = "🦐 Hải sản" },
                new SelectListItem { Value = "Chợ - Ẩm thực", Text = "🛒 Chợ - Ẩm thực" },
                new SelectListItem { Value = "Đi bộ",         Text = "🚶 Đi bộ" },
                new SelectListItem { Value = "Di tích",       Text = "🏛️ Di tích" },
                new SelectListItem { Value = "Thiên nhiên",   Text = "🌿 Thiên nhiên" },
                new SelectListItem { Value = "Điểm cao",      Text = "🗼 Điểm cao" },
                new SelectListItem { Value = "Nhà hàng",      Text = "🍽️ Nhà hàng" },
                new SelectListItem { Value = "Quán ăn",       Text = "🍜 Quán ăn" },
                new SelectListItem { Value = "Cafe",          Text = "☕ Cafe" },
                new SelectListItem { Value = "Bar - Pub",     Text = "🍺 Bar - Pub" },
                new SelectListItem { Value = "Ẩm thực",       Text = "🍱 Ẩm thực" }
            };
        }
    }
}
