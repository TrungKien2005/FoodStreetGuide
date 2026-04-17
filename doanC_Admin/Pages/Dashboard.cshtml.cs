using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace doanC_Admin.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly FoodStreetGuideDBContext _context;

        public DashboardModel(FoodStreetGuideDBContext context)
        {
            _context = context;
        }

        // ========== THỐNG KÊ CƠ BẢN ==========
        public int TotalLocations { get; set; }
        public int TotalPendingPOI { get; set; }
        public int TotalApprovedPOI { get; set; }
        public int TotalMovements { get; set; }
        public int TotalListens { get; set; }
        public double AvgListenTime { get; set; }

        // ========== REAL-TIME DASHBOARD ==========
        public int ActiveUsersNow { get; set; }
        public int TotalLoggedIn { get; set; }
        public int ActiveVisitorsNow { get; set; }
        public int ScansLastHour { get; set; }
        public int ScansToday { get; set; }
        public int TodayScans { get; set; }
        public int TodayVisitors { get; set; }
        public int TodayGeoFenceEntries { get; set; }
        public int TodayTTSPlays { get; set; }

        // ========== TĂNG TRƯỞNG ==========
        public double LocationGrowth { get; set; }
        public double MovementGrowth { get; set; }
        public double ListenGrowth { get; set; }
        public double AvgTimeGrowth { get; set; }

        // ========== PHÂN TRANG LỊCH SỬ ĐĂNG NHẬP ==========
        public int LoginHistoryPage { get; set; } = 1;
        public int LoginHistoryPageSize { get; set; } = 10;
        public int LoginHistoryTotalCount { get; set; }
        public int LoginHistoryTotalPages { get; set; }

        // ========== 1. LƯU CHUYỂN DI CHUYỂN ==========
        public List<string> MovementLabels { get; set; } = new();
        public List<int> MovementData { get; set; } = new();

        // ========== 2. TOP ĐỊA ĐIỂM ĐƯỢC NGHE ==========
        public List<string> TopListenedNames { get; set; } = new();
        public List<int> TopListenedCounts { get; set; } = new();

        // ========== 3. THỜI GIAN NGHE TRUNG BÌNH THEO POI ==========
        public List<string> AvgTimeLabels { get; set; } = new();
        public List<double> AvgTimeData { get; set; } = new();

        // ========== 4. HEATMAP ==========
        public List<HeatmapPoint> HeatmapPoints { get; set; } = new();
        public int HeatmapPointsCount { get; set; }

        // ========== 5. NGƯỜI ĐANG HOẠT ĐỘNG ==========
        public List<ActiveUserDto> ActiveUserList { get; set; } = new();

        // ========== 6. LỊCH SỬ ĐĂNG NHẬP ==========
        public List<LoginHistoryDto> LoginHistory { get; set; } = new();

        // ========== 7. ĐỊA ĐIỂM CHỜ DUYỆT ==========
        public List<PendingLocationDto> PendingLocations { get; set; } = new();
        public List<ActiveAdminSessionDto> ActiveAdminSessions { get; set; } = new();

        public async Task OnGetAsync()
        {
            // ============================================
            // THỐNG KÊ CƠ BẢN
            // ============================================
            TotalLocations = await _context.LocationPoints.CountAsync();
            TotalPendingPOI = await _context.LocationPoints.CountAsync(l => l.IsApproved == false);
            TotalApprovedPOI = TotalLocations - TotalPendingPOI;
            TotalMovements = await _context.GeoFenceLogs.CountAsync();
            TotalListens = await _context.TTSLogs.CountAsync();
            TodayTTSPlays = await _context.TTSLogs.CountAsync(t => t.PlayedAt >= DateTime.Today);

            var listenTimes = await _context.TTSLogs
                .Where(t => t.DurationSeconds.HasValue)
                .Select(t => t.DurationSeconds.Value)
                .ToListAsync();
            AvgListenTime = listenTimes.Any() ? listenTimes.Average() : 0;

            // ============================================
            // REAL-TIME DASHBOARD
            // ============================================
            TodayScans = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= DateTime.Today);
            ScansLastHour = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= DateTime.Now.AddHours(-1));
            TodayVisitors = await _context.QRScanLogs.Select(s => s.DeviceId).Distinct().CountAsync();

            ActiveUsersNow = await _context.AdminSessions
                .CountAsync(s => s.IsActive && s.LastActivity >= DateTime.Now.AddMinutes(-5));
            TotalLoggedIn = await _context.AdminUsers.CountAsync();

            // ============================================
            // TÍNH % TĂNG TRƯỞNG
            // ============================================
            var thisWeekStart = DateTime.Now.AddDays(-7).Date;
            var lastWeekStart = DateTime.Now.AddDays(-14).Date;
            var lastWeekEnd = DateTime.Now.AddDays(-7).Date;

            var thisWeekLocations = await _context.LocationPoints.Where(l => l.CreatedAt >= thisWeekStart).CountAsync();
            var lastWeekLocations = await _context.LocationPoints.Where(l => l.CreatedAt >= lastWeekStart && l.CreatedAt < lastWeekEnd).CountAsync();
            LocationGrowth = lastWeekLocations == 0 ? 0 : ((double)(thisWeekLocations - lastWeekLocations) / lastWeekLocations) * 100;

            var thisWeekMovements = await _context.GeoFenceLogs.Where(g => g.EnterTime >= thisWeekStart).CountAsync();
            var lastWeekMovements = await _context.GeoFenceLogs.Where(g => g.EnterTime >= lastWeekStart && g.EnterTime < lastWeekEnd).CountAsync();
            MovementGrowth = lastWeekMovements == 0 ? 0 : ((double)(thisWeekMovements - lastWeekMovements) / lastWeekMovements) * 100;

            var thisWeekListens = await _context.TTSLogs.Where(t => t.PlayedAt >= thisWeekStart).CountAsync();
            var lastWeekListens = await _context.TTSLogs.Where(t => t.PlayedAt >= lastWeekStart && t.PlayedAt < lastWeekEnd).CountAsync();
            ListenGrowth = lastWeekListens == 0 ? 0 : ((double)(thisWeekListens - lastWeekListens) / lastWeekListens) * 100;

            // ============================================
            // 1. LƯU CHUYỂN DI CHUYỂN (7 NGÀY QUA)
            // ============================================
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                MovementLabels.Add(date.ToString("dd/MM"));
                var count = await _context.GeoFenceLogs
                    .Where(g => g.EnterTime.HasValue && g.EnterTime.Value.Date == date)
                    .CountAsync();
                MovementData.Add(count);
            }

            // ============================================
            // 2. TOP 5 ĐỊA ĐIỂM ĐƯỢC NGHE NHIỀU NHẤT
            // ============================================
            var topListened = await _context.TTSLogs
                .GroupBy(t => t.PointId)
                .Select(g => new { PointId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            foreach (var item in topListened)
            {
                var location = await _context.LocationPoints
                    .Where(l => l.PointId == item.PointId)
                    .Select(l => l.Name)
                    .FirstOrDefaultAsync();
                TopListenedNames.Add(location ?? "Không xác định");
                TopListenedCounts.Add(item.Count);
            }

            // ============================================
            // 3. THỜI GIAN NGHE TRUNG BÌNH THEO POI
            // ============================================
            var avgTimes = await _context.TTSLogs
                .Where(t => t.DurationSeconds.HasValue)
                .GroupBy(t => t.PointId)
                .Select(g => new { PointId = g.Key, AvgTime = g.Average(t => t.DurationSeconds.Value) })
                .OrderByDescending(x => x.AvgTime)
                .Take(5)
                .ToListAsync();

            foreach (var item in avgTimes)
            {
                var location = await _context.LocationPoints
                    .Where(l => l.PointId == item.PointId)
                    .Select(l => l.Name)
                    .FirstOrDefaultAsync();
                AvgTimeLabels.Add(location ?? "Không xác định");
                AvgTimeData.Add(Math.Round(item.AvgTime, 1));
            }

            // ============================================
            // 4. HEATMAP VỊ TRÍ NGƯỜI DÙNG
            // ============================================
            var qrLocations = await _context.QRScanLogs
                .Where(q => q.Latitude.HasValue && q.Longitude.HasValue)
                .Select(q => new { q.Latitude, q.Longitude })
                .ToListAsync();

            var allPoints = new List<(double lat, double lng)>();
            foreach (var q in qrLocations)
            {
                if (q.Latitude.HasValue && q.Longitude.HasValue)
                    allPoints.Add((q.Latitude.Value, q.Longitude.Value));
            }

            var heatmapDict = new Dictionary<string, int>();
            foreach (var point in allPoints)
            {
                var key = $"{Math.Round(point.lat, 3)},{Math.Round(point.lng, 3)}";
                if (heatmapDict.ContainsKey(key))
                    heatmapDict[key]++;
                else
                    heatmapDict[key] = 1;
            }

            HeatmapPoints = heatmapDict.Select(kvp => new HeatmapPoint
            {
                lat = double.Parse(kvp.Key.Split(',')[0]),
                lng = double.Parse(kvp.Key.Split(',')[1]),
                intensity = kvp.Value
            }).ToList();
            HeatmapPointsCount = allPoints.Count;

            // ============================================
            // 5. NGƯỜI ĐANG HOẠT ĐỘNG (ADMIN)
            // ============================================
            ActiveUserList = await _context.AdminSessions
                .Where(s => s.IsActive && s.LastActivity >= DateTime.Now.AddMinutes(-5))
                .Select(s => new ActiveUserDto
                {
                    AdminId = s.AdminId,
                    Username = s.Username,
                    FullName = _context.AdminUsers.Where(u => u.AdminId == s.AdminId).Select(u => u.FullName).FirstOrDefault() ?? "",
                    Role = _context.AdminUsers.Where(u => u.AdminId == s.AdminId).Select(u => u.Role).FirstOrDefault() ?? "User",
                    LoginTime = s.LoginTime,
                    LastActivity = s.LastActivity,
                    MinutesInactive = (int)(DateTime.Now - s.LastActivity).TotalMinutes,
                    IPAddress = s.IPAddress ?? "Unknown",
                    SessionStatus = (DateTime.Now - s.LastActivity).TotalMinutes > 5 ? "Idle" : "Active"
                })
                .OrderByDescending(s => s.LastActivity)
                .ToListAsync();

            // ============================================
            // 6. LỊCH SỬ ĐĂNG NHẬP (CÓ PHÂN TRANG)
            // ============================================

            // Lấy tham số phân trang từ QueryString
            if (Request.Query.ContainsKey("page"))
            {
                int.TryParse(Request.Query["page"], out int page);
                LoginHistoryPage = page > 0 ? page : 1;
            }
            if (Request.Query.ContainsKey("pageSize"))
            {
                int.TryParse(Request.Query["pageSize"], out int pageSize);
                LoginHistoryPageSize = pageSize > 0 ? pageSize : 10;
            }

            // Đếm tổng số bản ghi
            LoginHistoryTotalCount = await _context.AdminLoginLogs.CountAsync();

            // Tính tổng số trang
            LoginHistoryTotalPages = (int)Math.Ceiling((double)LoginHistoryTotalCount / LoginHistoryPageSize);

            // Lấy dữ liệu theo trang
            var loginHistoryQuery = _context.AdminLoginLogs
                .Include(l => l.AdminUser)
                .OrderByDescending(l => l.LoginTime)
                .Skip((LoginHistoryPage - 1) * LoginHistoryPageSize)
                .Take(LoginHistoryPageSize)
                .Select(l => new LoginHistoryDto
                {
                    LogId = l.LogId,
                    Username = l.Username,
                    FullName = l.AdminUser != null ? l.AdminUser.FullName : "",
                    AdminId = l.AdminId,
                    LoginTime = l.LoginTime,
                    LogoutTime = l.LogoutTime,
                    IPAddress = l.IPAddress ?? "Unknown",
                    DeviceInfo = l.DeviceInfo ?? "Unknown",
                    Status = l.Status ?? "Success",
                    IsCurrentlyActive = _context.AdminSessions
                        .Any(s => s.AdminId == l.AdminId && s.IsActive == true && s.LastActivity >= DateTime.Now.AddMinutes(-5))
                });

            LoginHistory = await loginHistoryQuery.ToListAsync();

            // ============================================
            // 7. ĐỊA ĐIỂM CHỜ DUYỆT
            // ============================================
            var pendingLocs = await _context.LocationPoints
                .Where(l => l.IsApproved == false)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var loc in pendingLocs)
            {
                string ownerName = "Chủ quán";
                if (loc.OwnerId.HasValue)
                {
                    var owner = await _context.StoreOwners
                        .Where(o => o.OwnerId == loc.OwnerId)
                        .Select(o => o.AdminUser.Username)
                        .FirstOrDefaultAsync();
                    ownerName = owner ?? "Chủ quán";
                }

                PendingLocations.Add(new PendingLocationDto
                {
                    PointId = loc.PointId,
                    Name = loc.Name ?? "",
                    OwnerName = ownerName,
                    CreatedAt = loc.CreatedAt
                });
            }
            // Lấy danh sách admin đang hoạt động
            ActiveAdminSessions = await _context.AdminSessions
                .Where(s => s.IsActive && s.LastActivity >= DateTime.Now.AddMinutes(-5))
                .Join(_context.AdminUsers, s => s.AdminId, u => u.AdminId, (s, u) => new ActiveAdminSessionDto
                {
                    AdminId = s.AdminId,
                    Username = s.Username,
                    FullName = u.FullName ?? "",
                    Role = u.Role ?? "Viewer",
                    LoginTime = s.LoginTime,
                    LastActivity = s.LastActivity,
                    MinutesInactive = (int)(DateTime.Now - s.LastActivity).TotalMinutes,
                    IPAddress = s.IPAddress ?? "Unknown"
                })
                .OrderByDescending(s => s.LastActivity)
                .ToListAsync();
        }
    }

    public class HeatmapPoint
    {
        public double lat { get; set; }
        public double lng { get; set; }
        public int intensity { get; set; }
    }

    public class ActiveUserDto
    {
        public int AdminId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public int MinutesInactive { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public string SessionStatus { get; set; } = string.Empty;
    }

    public class LoginHistoryDto
    {
        public int LogId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int AdminId { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsCurrentlyActive { get; set; }
    }

    public class PendingLocationDto
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    public class ActiveAdminSessionDto
    {
        public int AdminId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public int MinutesInactive { get; set; }
        public string IPAddress { get; set; } = string.Empty;
    }
}