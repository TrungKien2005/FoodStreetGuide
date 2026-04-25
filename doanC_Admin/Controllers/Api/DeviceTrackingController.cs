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
                        IsActive = true,
                        TotalScans = 0,
                        TotalListens = 0
                    };
                    _context.DeviceTracking.Add(device);
                }
                else
                {
                    device.LastActivity = DateTime.Now;
                    device.IsActive = true;
                    if (!string.IsNullOrEmpty(request.DeviceName))
                        device.DeviceName = request.DeviceName;
                    if (!string.IsNullOrEmpty(request.Platform))
                        device.Platform = request.Platform;
                }

                await _context.SaveChangesAsync();

                // ✅ Gửi real-time update qua SignalR
                await _hubContext.Clients.All.SendAsync("RefreshDeviceList");
                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    "🟢 Thiết bị hoạt động", $"{device.DeviceName} đã kết nối", "success");

                return Ok(new { success = true, device });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("UpdateHeartbeat")]
        public async Task<IActionResult> UpdateHeartbeat([FromBody] HeartbeatRequest request)
        {
            try
            {
                var device = await _context.DeviceTracking
                    .FirstOrDefaultAsync(d => d.DeviceUniqueId == request.DeviceUniqueId);

                if (device != null)
                {
                    device.LastActivity = DateTime.Now;
                    device.IsActive = true;
                    await _context.SaveChangesAsync();

                    // ✅ Gửi refresh nhẹ, không spam notification
                    await _hubContext.Clients.All.SendAsync("RefreshDeviceList");

                    return Ok(new { success = true });
                }

                return NotFound(new { success = false, message = "Device not found" });
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
                var device = await _context.DeviceTracking
                    .FirstOrDefaultAsync(d => d.DeviceUniqueId == request.DeviceUniqueId);

                if (device != null)
                {
                    device.IsActive = false;
                    device.LastActivity = DateTime.Now;
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("RefreshDeviceList");
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                        "🔴 Thiết bị ngắt kết nối", $"{device.DeviceName} đã offline", "warning");
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
                    device.IsActive = true;

                    if (request.ActivityType == "QRScan")
                    {
                        device.TotalScans++;
                        await _hubContext.Clients.All.SendAsync("RefreshStats");
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "📱 QR Scan mới", $"{device.DeviceName} vừa quét QR code", "info");
                    }
                    else if (request.ActivityType == "TTSListen")
                    {
                        device.TotalListens++;
                        await _hubContext.Clients.All.SendAsync("RefreshStats");
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "🎧 Audio được nghe", $"{device.DeviceName} vừa nghe audio", "info");
                    }

                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("RefreshDeviceList");
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
            // ✅ Active nếu có hoạt động trong 2 phút gần đây
            var activeThreshold = DateTime.Now.AddMinutes(-2);

            var activeDevices = await _context.DeviceTracking
                .Where(d => d.IsActive == true && d.LastActivity >= activeThreshold)
                .OrderByDescending(d => d.LastActivity)
                .Select(d => new
                {
                    d.DeviceId,
                    d.DeviceUniqueId,
                    d.DeviceName,
                    d.Platform,
                    d.LastActivity,
                    d.TotalScans,
                    d.TotalListens,
                    Status = "🟢 Online",
                    LastActivityFormatted = d.LastActivity.ToString("HH:mm:ss dd/MM")
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
                var activeThreshold = DateTime.Now.AddMinutes(-2);

                // ✅ Thiết bị online (có hoạt động trong 2 phút)
                var activeDevices = await _context.DeviceTracking
                    .CountAsync(d => d.IsActive == true && d.LastActivity >= activeThreshold);

                // ✅ Tổng số thiết bị từng kết nối
                var totalDevices = await _context.DeviceTracking.CountAsync();

                // ✅ Thống kê từ QRScanLogs
                var totalScans = await _context.QRScanLogs.CountAsync();
                var todayScans = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= today);
                var scansLastHour = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= DateTime.Now.AddHours(-1));

                // ✅ Thống kê từ TTSLogs  
                var totalListens = await _context.TTSLogs.CountAsync();
                var todayListens = await _context.TTSLogs.CountAsync(t => t.PlayedAt >= today);
                var listensLastHour = await _context.TTSLogs.CountAsync(t => t.PlayedAt >= DateTime.Now.AddHours(-1));

                // ✅ Thống kê thời gian nghe trung bình
                var listenTimes = await _context.TTSLogs
                    .Where(t => t.DurationSeconds.HasValue && t.DurationSeconds > 0)
                    .Select(t => (double)t.DurationSeconds.Value)
                    .ToListAsync();
                var avgListenSeconds = listenTimes.Any() ? listenTimes.Average() : 0;

                var stats = new
                {
                    activeDevices = activeDevices,
                    totalDevices = totalDevices,
                    totalScans = totalScans,
                    todayScans = todayScans,
                    scansLastHour = scansLastHour,
                    totalListens = totalListens,
                    todayListens = todayListens,
                    listensLastHour = listensLastHour,
                    avgListenSeconds = Math.Round(avgListenSeconds, 1),
                    updatedAt = DateTime.Now
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("SyncDeviceTracking")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncDeviceTracking()
        {
            try
            {
                // ✅ Đồng bộ dữ liệu từ logs vào DeviceTracking
                var devices = await _context.QRScanLogs
                    .Where(s => !string.IsNullOrEmpty(s.DeviceId))
                    .GroupBy(s => s.DeviceId)
                    .Select(g => new
                    {
                        DeviceUniqueId = g.Key,
                        TotalScans = g.Count(),
                        LastScan = g.Max(s => s.ScanTime)
                    })
                    .ToListAsync();

                foreach (var device in devices)
                {
                    var existing = await _context.DeviceTracking
                        .FirstOrDefaultAsync(d => d.DeviceUniqueId == device.DeviceUniqueId);

                    var totalListens = await _context.TTSLogs
                        .CountAsync(t => t.DeviceId != null && t.DeviceId == device.DeviceUniqueId);

                    if (existing == null)
                    {
                        _context.DeviceTracking.Add(new DeviceTracking
                        {
                            DeviceUniqueId = device.DeviceUniqueId,
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
                await _hubContext.Clients.All.SendAsync("RefreshDeviceList");
                await _hubContext.Clients.All.SendAsync("RefreshStats");

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

    public class HeartbeatRequest
    {
        public string DeviceUniqueId { get; set; } = string.Empty;
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