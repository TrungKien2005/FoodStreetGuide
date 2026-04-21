using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using doanC_Admin.Models;
using doanC_Admin.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class DeviceTrackingController : ControllerBase
    {
        private readonly FoodStreetGuideDBContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public DeviceTrackingController(FoodStreetGuideDBContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost("Track")]
        public async Task<IActionResult> TrackDevice([FromBody] DeviceTrackingRequest request)
        {
            try
            {
                var device = await _context.DeviceTracking
                    .FirstOrDefaultAsync(d => d.DeviceUniqueId == request.DeviceUniqueId);

                if (device == null)
                {
                    device = new DeviceTracking
                    {
                        DeviceUniqueId = request.DeviceUniqueId,
                        DeviceName = request.DeviceName,
                        Platform = request.Platform,
                        OSVersion = request.OsVersion,
                        AppVersion = request.AppVersion,
                        FirstSeen = DateTime.Now,
                        LastActivity = DateTime.Now,
                        IsActive = true
                    };
                    _context.DeviceTracking.Add(device);

                    await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                        "🆕 Thiết bị mới", $"Thiết bị {request.DeviceName} vừa kết nối!", "info");
                }
                else
                {
                    device.LastActivity = DateTime.Now;
                    device.IsActive = true;
                    if (!string.IsNullOrEmpty(request.DeviceName))
                        device.DeviceName = request.DeviceName;
                }

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("RefreshDeviceList");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Untrack")]
        public async Task<IActionResult> UntrackDevice([FromBody] UntrackRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.DeviceUniqueId))
                    return BadRequest(new { success = false, message = "deviceUniqueId required" });

                var device = await _context.DeviceTracking
                    .FirstOrDefaultAsync(d => d.DeviceUniqueId == request.DeviceUniqueId);

                if (device != null)
                {
                    device.IsActive = false;
                    device.LastActivity = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Notify clients to refresh device list immediately
                    await _hubContext.Clients.All.SendAsync("RefreshDeviceList");
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                        "📴 Thiết bị ngắt kết nối", $"Thiết bị {device.DeviceName ?? request.DeviceUniqueId} đã ngắt kết nối", "info");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("TrackActivity")]
        public async Task<IActionResult> TrackActivity([FromBody] ActivityTrackingRequest request)
        {
            try
            {
                var device = await _context.DeviceTracking
                    .FirstOrDefaultAsync(d => d.DeviceUniqueId == request.DeviceUniqueId);

                if (device != null)
                {
                    device.LastActivity = DateTime.Now;

                    if (request.ActivityType == "QRScan")
                    {
                        device.TotalScans++;

                        await _hubContext.Clients.All.SendAsync("ReceiveNewData", "QRScan", device.TotalScans);
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "📱 QR Code mới", $"Có 1 lượt quét mới từ {device.DeviceName}!", "success");
                        await _hubContext.Clients.All.SendAsync("RefreshDashboard");
                    }
                    else if (request.ActivityType == "TTSListen")
                    {
                        device.TotalListens++;

                        await _hubContext.Clients.All.SendAsync("ReceiveNewData", "TTSListen", device.TotalListens);
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "🎧 Audio được nghe", $"Đã có 1 lượt nghe từ {device.DeviceName}!", "info");
                    }
                    else if (request.ActivityType == "ViewList")
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "👁️ Xem danh sách", $"Người dùng đang xem danh sách địa điểm", "info");
                    }
                    else if (request.ActivityType == "ViewDetail")
                    {
                        var point = await _context.LocationPoints
                            .Where(p => p.PointId == request.PointId)
                            .Select(p => p.Name)
                            .FirstOrDefaultAsync();

                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "📍 Xem chi tiết", $"Đang xem: {point ?? "địa điểm"}", "info");
                    }

                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("RefreshStats");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetActiveDevices")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveDevices()
        {
            // Changed: consider active devices within last 30 seconds for faster result
            var activeDevices = await _context.DeviceTracking
                .Where(d => d.IsActive && d.LastActivity >= DateTime.Now.AddSeconds(-30))
                .OrderByDescending(d => d.LastActivity)
                .Select(d => new
                {
                    d.DeviceId,
                    d.DeviceUniqueId,
                    d.DeviceName,
                    d.Platform,
                    d.LastActivity,
                    d.TotalScans,
                    d.TotalListens
                })
                .ToListAsync();

            return Ok(activeDevices);
        }

        [HttpGet("GetStats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var today = DateTime.Today;
                var last30Seconds = DateTime.Now.AddSeconds(-30);
                var last30Minutes = DateTime.Now.AddMinutes(-30);
                var totalScans = await _context.QRScanLogs.CountAsync();
                var todayScans = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= today);
                var scansLastHour = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= DateTime.Now.AddHours(-1));

                // Số thiết bị hoạt động trong 30 phút qua
                var activeDevices = await _context.QRScanLogs
                    .Where(s => s.ScanTime >= last30Minutes)
                    .Select(s => s.DeviceId)
                    .Distinct()
                    .CountAsync();

                // Tổng số thiết bị từng ghi nhận
                var totalDevices = await _context.QRScanLogs
                    .Select(s => s.DeviceId)
                    .Distinct()
                    .CountAsync();

                var totalListens = await _context.TTSLogs.CountAsync();
                var todayListens = await _context.TTSLogs.CountAsync(t => t.PlayedAt >= today);
                var listensLastHour = await _context.TTSLogs.CountAsync(t => t.PlayedAt >= DateTime.Now.AddHours(-1));

                // Thời gian nghe trung bình
                var avgListenTime = 0.0;
                var listenTimes = await _context.TTSLogs
                    .Where(t => t.DurationSeconds.HasValue)
                    .Select(t => t.DurationSeconds.Value)
                    .ToListAsync();
                if (listenTimes.Any())
                {
                    avgListenTime = listenTimes.Average();
                }
                var activeAdmins = await _context.AdminSessions
                    .CountAsync(s => s.IsActive && s.LastActivity >= DateTime.Now.AddMinutes(-5));

                var totalAdmins = await _context.AdminUsers.CountAsync();
                var deviceTrackingCount = await _context.DeviceTracking.CountAsync();
                var activeDeviceTracking = await _context.DeviceTracking
                    .CountAsync(d => d.IsActive && d.LastActivity >= last30Seconds);

                var stats = new
                {
                    // Thống kê từ QRScanLogs
                    totalScans = totalScans,
                    todayScans = todayScans,
                    scansLastHour = scansLastHour,
                    activeDevices = activeDevices,
                    totalDevices = totalDevices,

                    // Thống kê từ TTSLogs
                    totalListens = totalListens,
                    todayListens = todayListens,
                    listensLastHour = listensLastHour,
                    avgListenTime = Math.Round(avgListenTime, 1),

                    // Thống kê Admin
                    activeAdmins = activeAdmins,
                    totalAdmins = totalAdmins,

                    // Thống kê từ DeviceTracking (cũ, để tương thích)
                    deviceTrackingCount = deviceTrackingCount,
                    activeDeviceTracking = activeDeviceTracking,

                    // Thời gian cập nhật
                    updatedAt = DateTime.Now
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetStats] Error: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetActiveUsers")]
        public async Task<IActionResult> GetActiveUsers()
        {
            var users = await _context.AdminSessions
                .Where(s => s.IsActive && s.LastActivity >= DateTime.Now.AddMinutes(-5))
                .Select(s => new
                {
                    s.AdminId,
                    s.Username,
                    s.LastActivity,
                    s.IPAddress
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("SyncDeviceTracking")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncDeviceTracking()
        {
            try
            {
                // Lấy tất cả thiết bị từ QRScanLogs (DeviceId là string)
                var devices = await _context.QRScanLogs
                    .Where(s => !string.IsNullOrEmpty(s.DeviceId))
                    .GroupBy(s => s.DeviceId)
                    .Select(g => new
                    {
                        DeviceUniqueId = g.Key,           // string - đây là ID từ QRScanLogs
                        TotalScans = g.Count(),
                        LastScan = g.Max(s => s.ScanTime)
                    })
                    .ToListAsync();

                foreach (var device in devices)
                {
                    // ✅ ĐÚNG: Tìm theo DeviceUniqueId (string so sánh với string)
                    var existing = await _context.DeviceTracking
                        .FirstOrDefaultAsync(d => d.DeviceUniqueId == device.DeviceUniqueId);

                    var totalListens = await _context.TTSLogs
                        .CountAsync(t => t.DeviceId != null && t.DeviceId == device.DeviceUniqueId);

                    if (existing == null)
                    {
                        // ✅ ĐÚNG: Không gán DeviceId (int), để database tự tạo
                        _context.DeviceTracking.Add(new DeviceTracking
                        {
                            DeviceUniqueId = device.DeviceUniqueId,  // Gán string vào DeviceUniqueId
                            TotalScans = device.TotalScans,
                            TotalListens = totalListens,
                            LastActivity = device.LastScan,
                            FirstSeen = device.LastScan,
                            IsActive = device.LastScan >= DateTime.Now.AddMinutes(-30),
                            Platform = "Unknown",
                            DeviceName = device.DeviceUniqueId
                        });
                    }
                    else
                    {
                        existing.TotalScans = device.TotalScans;
                        existing.TotalListens = totalListens;
                        existing.LastActivity = device.LastScan;
                        existing.IsActive = device.LastScan >= DateTime.Now.AddMinutes(-30);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Đã đồng bộ {devices.Count} thiết bị" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class DeviceTrackingRequest
    {
        public string DeviceUniqueId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public double? LastLocationLat { get; set; }
        public double? LastLocationLng { get; set; }
    }

    public class ActivityTrackingRequest
    {
        public string DeviceUniqueId { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public int PointId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UntrackRequest
    {
        public string DeviceUniqueId { get; set; } = string.Empty;
    }
}