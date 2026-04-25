using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace doanC_Admin.Pages.Owner
{
    public class CreateLocationModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateLocationModel(FoodStreetGuideDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public LocationPoint LocationPoint { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Manager")
                return RedirectToPage("/Dashboard");

            LoadCategories();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var admin = await _context.AdminUsers.FindAsync(int.Parse(adminId));
            var storeOwner = await _context.StoreOwners.FirstOrDefaultAsync(s => s.AdminId == admin.AdminId);
            var ownerId = storeOwner?.OwnerId ?? 0;

            LocationPoint.OwnerId = ownerId;
            LocationPoint.CreatedBy = admin.AdminId;
            LocationPoint.IsApproved = false;  // 👈 CHỜ DUYỆT
            LocationPoint.CreatedAt = DateTime.Now;
            LocationPoint.UpdatedAt = DateTime.Now;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{ImageFile.FileName}";
                var imagesPath = GetImagesPath();
                var filePath = Path.Combine(imagesPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                LocationPoint.Image = fileName;
            }

            _context.LocationPoints.Add(LocationPoint);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Địa điểm đã được gửi đi và đang chờ admin duyệt!";
            return RedirectToPage("/Owner/Dashboard");
        }

        private string GetImagesPath()
        {
            var imagesPath = Path.Combine(_environment.WebRootPath, "images", "locations");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);
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
                new SelectListItem { Value = "Nhà hàng", Text = "🍽️ Nhà hàng" },
                new SelectListItem { Value = "Quán ăn", Text = "🍜 Quán ăn" }
            };
        }
    }
}

