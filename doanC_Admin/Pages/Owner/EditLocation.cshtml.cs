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
using doanC_Admin.Helpers;
using Microsoft.AspNetCore.Authorization;  // âœ… THĂM DĂ’NG NĂ€Y

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

        [ValidateAntiForgeryToken]  // âœ… THĂM DĂ’NG NĂ€Y
        public async Task<IActionResult> OnPostAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            // âœ… THĂM VALIDATION
            if (string.IsNullOrWhiteSpace(LocationPoint.Name))
            {
                ErrorMessage = "TĂªn Ä‘á»‹a Ä‘iá»ƒm khĂ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng";
                LoadCategories();
                return Page();
            }

            if (LocationPoint.Latitude == 0 || LocationPoint.Longitude == 0)
            {
                ErrorMessage = "Vui lĂ²ng nháº­p tá»a Ä‘á»™ há»£p lá»‡ (kinh Ä‘á»™, vÄ© Ä‘á»™)";
                LoadCategories();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(LocationPoint.Category))
            {
                ErrorMessage = "Vui lĂ²ng chá»n danh má»¥c";
                LoadCategories();
                return Page();
            }

            var existingLocation = await _context.LocationPoints.FindAsync(LocationPoint.PointId);
            if (existingLocation == null || existingLocation.OwnerId != storeOwner.OwnerId)
            {
                return RedirectToPage("/Owner/MyLocations");
            }

            // Cáº­p nháº­t thĂ´ng tin
            existingLocation.Name = LocationPoint.Name;
            existingLocation.Category = LocationPoint.Category;
            existingLocation.Description = LocationPoint.Description;
            existingLocation.Address = LocationPoint.Address;
            existingLocation.Latitude = LocationPoint.Latitude;
            existingLocation.Longitude = LocationPoint.Longitude;
            existingLocation.OpeningHours = LocationPoint.OpeningHours;
            existingLocation.PriceRange = LocationPoint.PriceRange;

            // QUAN TRá»ŒNG: Äáº·t láº¡i IsApproved = false Ä‘á»ƒ Admin duyá»‡t láº¡i
            existingLocation.IsApproved = false;
            existingLocation.UpdatedAt = DateTime.Now;

            // âœ… Sá»¬A: Xá»­ lĂ½ áº£nh - XĂ³a áº£nh cÅ© náº¿u cĂ³ áº£nh má»›i
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // XĂ³a áº£nh cÅ©
                if (!string.IsNullOrEmpty(existingLocation.Image))
                {
                    var oldImagePath = Path.Combine(GetImagesPath(), existingLocation.Image);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // LÆ°u áº£nh má»›i
                var fileName = $"{Guid.NewGuid()}_{ImageFile.FileName}";
                var filePath = Path.Combine(GetImagesPath(), fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                existingLocation.Image = fileName;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cáº­p nháº­t thĂ nh cĂ´ng! Äá»‹a Ä‘iá»ƒm Ä‘Ă£ Ä‘Æ°á»£c gá»­i láº¡i cho Admin chá» phĂª duyá»‡t.";
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
            Categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "Ä‚n váº·t", Text = "đŸ¢ Ä‚n váº·t" },
                new SelectListItem { Value = "Äá»“ uá»‘ng", Text = "â˜• Äá»“ uá»‘ng" },
                new SelectListItem { Value = "Äá»“ nÆ°á»›ng", Text = "đŸ”¥ Äá»“ nÆ°á»›ng" },
                new SelectListItem { Value = "Háº£i sáº£n", Text = "đŸ¦ Háº£i sáº£n" },
                new SelectListItem { Value = "NhĂ  hĂ ng", Text = "đŸ½ï¸ NhĂ  hĂ ng" },
                new SelectListItem { Value = "QuĂ¡n Äƒn", Text = "đŸœ QuĂ¡n Äƒn" },
                new SelectListItem { Value = "Cafe", Text = "â˜• Cafe" }
            };
        }
    }
}

