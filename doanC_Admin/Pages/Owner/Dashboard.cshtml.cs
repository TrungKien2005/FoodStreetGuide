using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using doanC_Admin.Helpers;

namespace doanC_Admin.Pages.Owner
{
    [Authorize("Owner", "Manager")]
    public class DashboardModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public DashboardModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        public string OwnerName { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public int MyLocationsCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int TotalScans { get; set; }
        public int TotalListens { get; set; }
        public int UniqueVisitors { get; set; }

        public List<OwnerLocationDto> MyLocations { get; set; } = new();
        public List<OwnerAudioDto> MyAudios { get; set; } = new();
        public List<LocationPoint> ApprovedLocations { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Login");

            var role = HttpContext.Session.GetString("Role");

            // ✅ SỬA: Cho phép cả Owner và Manager
            if (role != "Owner" && role != "Manager")
                return RedirectToPage("/Dashboard");

            OwnerId = int.Parse(adminId);
            var admin = await _context.AdminUsers.FindAsync(OwnerId);
            OwnerName = admin?.FullName ?? admin?.Username ?? "Chủ quán";

            // ✅ Lấy StoreOwner
            var storeOwner = await _context.StoreOwners.FirstOrDefaultAsync(s => s.AdminId == OwnerId);

            // Nếu chưa có StoreOwner, tạo mới
            if (storeOwner == null)
            {
                storeOwner = new StoreOwner
                {
                    AdminId = OwnerId,
                    StoreName = OwnerName,
                    Status = "Approved",
                    CreatedAt = DateTime.Now
                };
                _context.StoreOwners.Add(storeOwner);
                await _context.SaveChangesAsync();
            }

            var ownerId = storeOwner.OwnerId;

            // ✅ Lấy danh sách địa điểm
            var locations = await _context.LocationPoints
                .Where(l => l.OwnerId == ownerId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            MyLocationsCount = locations.Count;
            ApprovedCount = locations.Count(l => l.IsApproved);
            PendingCount = locations.Count(l => !l.IsApproved);

            ApprovedLocations = locations.Where(l => l.IsApproved).ToList();

            // ✅ Xóa vòng lặp trùng lặp
            MyLocations.Clear();
            TotalScans = 0;
            TotalListens = 0;

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
                    Category = loc.Category ?? "",
                    IsApproved = loc.IsApproved,
                    TotalScans = scans,
                    TotalListens = listens
                });
            }

            // ✅ Lấy Audio
            var locationDict = locations.ToDictionary(l => l.PointId, l => l.Name ?? "");

            MyAudios = await _context.AudioFiles
                .Where(a => locations.Select(l => l.PointId).Contains(a.PointId))
                .Join(_context.Languages, a => a.LanguageId, l => l.LanguageId, (a, l) => new OwnerAudioDto
                {
                    AudioId = a.AudioId,
                    PointId = a.PointId,
                    LocationName = locationDict.ContainsKey(a.PointId) ? locationDict[a.PointId] : "",
                    FileName = a.FileName ?? "",
                    Language = l.LanguageName ?? "Tiếng Việt",
                    FilePath = a.FilePath ?? "",
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Page();
        }
    }

    public class OwnerLocationDto
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public int TotalScans { get; set; }
        public int TotalListens { get; set; }
    }

    public class OwnerAudioDto
    {
        public int AudioId { get; set; }
        public int PointId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}