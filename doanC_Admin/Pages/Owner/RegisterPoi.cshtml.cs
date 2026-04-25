using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using doanC_Admin.Helpers;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace doanC_Admin.Pages.Owner
{
    [doanC_Admin.Helpers.Authorize("Owner", "Manager")]
    public class RegisterPoiModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RegisterPoiModel(FoodStreetGuideDBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public LocationPoint Location { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var adminId = HttpContext.Session.GetString("AdminId");
                if (string.IsNullOrEmpty(adminId))
                    return RedirectToPage("/Login");

                var storeOwner = await _context.StoreOwners
                    .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

                if (storeOwner == null)
                {
                    ErrorMessage = "Bạn chưa được cấp quyền chủ quán";
                    return Page();
                }

                // Validate
                if (string.IsNullOrEmpty(Location.Name))
                {
                    ErrorMessage = "Vui lòng nhập tên địa điểm";
                    return Page();
                }

                if (Location.Latitude == 0 || Location.Longitude == 0)
                {
                    ErrorMessage = "Vui lòng chọn vị trí trên bản đồ để tự động lấy tọa độ (kinh độ, vĩ độ)";
                    return Page();
                }

                if (string.IsNullOrEmpty(Location.Category))
                {
                    ErrorMessage = "Vui lòng chọn danh mục";
                    return Page();
                }

                // Handle image upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "locations");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{ImageFile.FileName}";
                    var filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    Location.Image = uniqueFileName;
                }

                Location.OwnerId = storeOwner.OwnerId;
                Location.IsApproved = false;
                Location.CreatedAt = DateTime.Now;
                Location.UpdatedAt = DateTime.Now;

                _context.LocationPoints.Add(Location);
                await _context.SaveChangesAsync();

                // Update pending count
                storeOwner.PendingLocations++;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã gửi yêu cầu đăng ký địa điểm, vui lòng chờ Admin duyệt!";
                return RedirectToPage("/Owner/MyLocations");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
                return Page();
            }
        }
    }
}
