using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using doanC_Admin.Helpers;

namespace doanC_Admin.Pages.Owner
{
    [Authorize("Owner")]
    public class MyAudiosModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public MyAudiosModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        // ✅ KHAI BÁO PROPERTIES
        public List<LocationPoint> ApprovedLocations { get; set; } = new();
        public Dictionary<int, List<AudioFile>> AudiosByLocation { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            // Lấy các địa điểm đã được duyệt của Owner
            ApprovedLocations = await _context.LocationPoints
                .Where(l => l.OwnerId == storeOwner.OwnerId && l.IsApproved == true)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Lấy audio theo từng địa điểm
            foreach (var loc in ApprovedLocations)
            {
                var audios = await _context.AudioFiles
                    .Where(a => a.PointId == loc.PointId)
                    .Include(a => a.Language)
                    .ToListAsync();

                AudiosByLocation[loc.PointId] = audios;
            }

            return Page();
        }
    }
}