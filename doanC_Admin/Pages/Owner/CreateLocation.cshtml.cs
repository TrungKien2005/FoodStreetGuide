using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
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
        private const int MAX_POI_LIMIT = 2;

        public CreateLocationModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LocationPoint LocationPoint { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public int RemainingSlots { get; set; }
        public int MaxPOILimit => MAX_POI_LIMIT;

        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Manager")
                return RedirectToPage("/Dashboard");

            await CheckRemainingSlotsAsync();
            LoadCategories();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            await CheckRemainingSlotsAsync();
            if (RemainingSlots <= 0)
            {
                ModelState.AddModelError(string.Empty, "Bạn đã đạt giới hạn địa điểm");
                LoadCategories();
                return Page();
            }

            // Lấy OwnerId
            var admin = await _context.AdminUsers.FindAsync(int.Parse(adminId));
            if (admin == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy thông tin người dùng");
                LoadCategories();
                return Page();
            }

            var storeOwner = await _context.StoreOwners.FirstOrDefaultAsync(s => s.AdminId == admin.AdminId);
            var ownerId = storeOwner?.OwnerId ?? 0;

            LocationPoint.OwnerId = ownerId;
            LocationPoint.CreatedBy = admin.AdminId;
            LocationPoint.IsApproved = false;
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

            return RedirectToPage("/Owner/Dashboard");
        }

        private async Task CheckRemainingSlotsAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                RemainingSlots = MAX_POI_LIMIT;
                return;
            }

            var storeOwner = await _context.StoreOwners.FirstOrDefaultAsync(s => s.AdminId == int.Parse(adminId));
            var ownerId = storeOwner?.OwnerId ?? 0;
            var currentCount = await _context.LocationPoints.CountAsync(l => l.OwnerId == ownerId);
            RemainingSlots = MAX_POI_LIMIT - currentCount;
            if (RemainingSlots < 0) RemainingSlots = 0;
        }

        private string GetImagesPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
            var imagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "Images");
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
                new SelectListItem { Value = "Quán ăn", Text = "🍜 Quán ăn" },
                new SelectListItem { Value = "Cafe", Text = "☕ Cafe" },
                new SelectListItem { Value = "Bar - Pub", Text = "🍺 Bar - Pub" }
            };
        }
    }
}