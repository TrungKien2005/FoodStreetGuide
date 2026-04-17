using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace doanC_Admin.Pages.Owner
{
    public class OwnerDashboardModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;
        private const int MAX_POI_LIMIT = 2;

        public OwnerDashboardModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public string OwnerName { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public int MyLocationsCount { get; set; }
        public int MaxPOILimit => MAX_POI_LIMIT;
        public int TotalScans { get; set; }
        public int TotalListens { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }

        public List<OwnerLocationDto> MyLocations { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Manager")
                return RedirectToPage("/Dashboard");

            OwnerId = int.Parse(adminId);
            var admin = await _context.AdminUsers.FindAsync(OwnerId);
            OwnerName = admin?.FullName ?? admin?.Username ?? "Chủ quán";

            // Lấy danh sách địa điểm của chủ quán
            var storeOwner = await _context.StoreOwners.FirstOrDefaultAsync(s => s.AdminId == OwnerId);
            var ownerId = storeOwner?.OwnerId ?? 0;

            MyLocationsCount = await _context.LocationPoints.CountAsync(l => l.OwnerId == ownerId);
            ApprovedCount = await _context.LocationPoints.CountAsync(l => l.OwnerId == ownerId && l.IsApproved);
            PendingCount = MyLocationsCount - ApprovedCount;

            var locations = await _context.LocationPoints
                .Where(l => l.OwnerId == ownerId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            foreach (var loc in locations)
            {
                var scans = await _context.QRScanLogs.CountAsync(s => s.PointId == loc.PointId);
                var listens = await _context.TTSLogs.CountAsync(t => t.PointId == loc.PointId);

                TotalScans += scans;
                TotalListens += listens;

                MyLocations.Add(new OwnerLocationDto
                {
                    PointId = loc.PointId,
                    Name = loc.Name ?? "",
                    IsApproved = loc.IsApproved,
                    TotalScans = scans,
                    TotalListens = listens
                });
            }

            return Page();
        }
    }

    public class OwnerLocationDto
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public int TotalScans { get; set; }
        public int TotalListens { get; set; }
    }
}