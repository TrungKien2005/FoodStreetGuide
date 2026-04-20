using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using doanC_Admin.Helpers;

namespace doanC_Admin.Pages.Owner
{
    [Authorize("Owner")]
    public class StatisticsModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public StatisticsModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public List<string> ScanLabels { get; set; } = new();
        public List<int> ScanData { get; set; } = new();
        public List<string> ListenLabels { get; set; } = new();
        public List<int> ListenData { get; set; } = new();
        public List<string> TopLocationNames { get; set; } = new();
        public List<int> TopLocationTotals { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var storeOwner = await _context.StoreOwners
                .FirstOrDefaultAsync(o => o.AdminId == int.Parse(adminId));

            if (storeOwner == null)
                return RedirectToPage("/Error");

            var ownerLocations = await _context.LocationPoints
                .Where(l => l.OwnerId == storeOwner.OwnerId && l.IsApproved == true)
                .Select(l => l.PointId)
                .ToListAsync();

            // 7 days scan stats
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                ScanLabels.Add(date.ToString("dd/MM"));
                var count = await _context.QRScanLogs
                    .Where(s => ownerLocations.Contains(s.PointId) && s.ScanTime.Date == date)
                    .CountAsync();
                ScanData.Add(count);
            }

            // 7 days listen stats
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                ListenLabels.Add(date.ToString("dd/MM"));
                var count = await _context.TTSLogs
                    .Where(t => ownerLocations.Contains(t.PointId) && t.PlayedAt.Date == date)
                    .CountAsync();
                ListenData.Add(count);
            }

            // Top locations
            var topLocations = await _context.LocationPoints
                .Where(l => l.OwnerId == storeOwner.OwnerId && l.IsApproved == true)
                .Select(l => new
                {
                    l.Name,
                    Total = _context.QRScanLogs.Count(s => s.PointId == l.PointId) +
                            _context.TTSLogs.Count(t => t.PointId == l.PointId)
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync();

            foreach (var loc in topLocations)
            {
                TopLocationNames.Add(loc.Name ?? "Không xác định");
                TopLocationTotals.Add(loc.Total);
            }

            return Page();
        }
    }
}