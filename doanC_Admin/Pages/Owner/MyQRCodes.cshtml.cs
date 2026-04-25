using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using doanC_Admin.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace doanC_Admin.Pages.Owner
{
    [doanC_Admin.Helpers.Authorize("Owner", "Manager")]
    public class MyQRCodesModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public MyQRCodesModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<LocationPoint> ApprovedLocations { get; set; } = new();
        [BindProperty]
        public int SelectedPointId { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            // Láº¥y cĂ¡c Ä‘á»‹a Ä‘iá»ƒm Ä‘Ă£ Ä‘Æ°á»£c duyá»‡t cá»§a Owner
            ApprovedLocations = await _context.LocationPoints
                .Where(l => l.OwnerId == storeOwner.OwnerId && l.IsApproved == true)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return Page();
        }
        public async Task<IActionResult> OnPostGenerateAsync(int pointId)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            var location = await _context.LocationPoints
                .FirstOrDefaultAsync(l => l.PointId == pointId && l.OwnerId == storeOwner.OwnerId);

            if (location == null)
                return NotFound();

            // Logic táº¡o QR code á»Ÿ Ä‘Ă¢y
            // ...

            TempData["SuccessMessage"] = $"QR Code cho {location.Name} Ä‘Ă£ Ä‘Æ°á»£c táº¡o!";
            return RedirectToPage();
        }
    }
}

