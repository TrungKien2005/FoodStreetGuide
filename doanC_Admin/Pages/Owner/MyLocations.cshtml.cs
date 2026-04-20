using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using doanC_Admin.Helpers;

namespace doanC_Admin.Pages.Owner
{
    [Authorize("Owner")]
    public class MyLocationsModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public MyLocationsModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<OwnerLocationItem> MyLocations { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            var locations = await _context.LocationPoints
                .Where(l => l.OwnerId == storeOwner.OwnerId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            foreach (var loc in locations)
            {
                var scans = await _context.QRScanLogs.CountAsync(s => s.PointId == loc.PointId);
                var listens = await _context.TTSLogs.CountAsync(t => t.PointId == loc.PointId);
                var todayVisitors = await _context.QRScanLogs
                    .CountAsync(s => s.PointId == loc.PointId && s.ScanTime >= DateTime.Today);

                MyLocations.Add(new OwnerLocationItem
                {
                    PointId = loc.PointId,
                    Name = loc.Name ?? "",
                    Category = loc.Category ?? "",
                    Address = loc.Address ?? "",
                    IsApproved = loc.IsApproved,
                    RejectionReason = loc.RejectionReason,
                    TotalScans = scans,
                    TotalListens = listens,
                    TodayVisitors = todayVisitors
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            var location = await _context.LocationPoints
                .FirstOrDefaultAsync(l => l.PointId == id && l.OwnerId == storeOwner.OwnerId);

            if (location != null)
            {
                _context.LocationPoints.Remove(location);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }

    // ✅ ĐỔI TÊN CLASS ĐỂ TRÁNH TRÙNG
    public class OwnerLocationItem
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
        public int TotalScans { get; set; }
        public int TotalListens { get; set; }
        public int TodayVisitors { get; set; }
    }
}