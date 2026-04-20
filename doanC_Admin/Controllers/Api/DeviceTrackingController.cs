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

        // 👈 SỬA CONSTRUCTOR
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

                    // 👈 GỬI THÔNG BÁO THIẾT BỊ MỚI
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

                // 👈 GỬI CẬP NHẬT DANH SÁCH THIẾT BỊ
                await _hubContext.Clients.All.SendAsync("RefreshDeviceList");

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

                        // 👈 GỬI REAL-TIME KHI CÓ QR SCAN
                        await _hubContext.Clients.All.SendAsync("ReceiveNewData", "QRScan", device.TotalScans);
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "📱 QR Code mới", $"Có 1 lượt quét mới từ {device.DeviceName}!", "success");
                        await _hubContext.Clients.All.SendAsync("RefreshDashboard");
                    }
                    else if (request.ActivityType == "TTSListen")
                    {
                        device.TotalListens++;

                        // 👈 GỬI REAL-TIME KHI CÓ TTS LISTEN
                        await _hubContext.Clients.All.SendAsync("ReceiveNewData", "TTSListen", device.TotalListens);
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "🎧 Audio được nghe", $"Đã có 1 lượt nghe từ {device.DeviceName}!", "info");
                    }
                    else if (request.ActivityType == "ViewList")
                    {
                        // 👈 GỬI REAL-TIME KHI XEM DANH SÁCH
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "👁️ Xem danh sách", $"Người dùng đang xem danh sách địa điểm", "info");
                    }
                    else if (request.ActivityType == "ViewDetail")
                    {
                        // 👈 GỬI REAL-TIME KHI XEM CHI TIẾT
                        var point = await _context.LocationPoints
                            .Where(p => p.PointId == request.PointId)
                            .Select(p => p.Name)
                            .FirstOrDefaultAsync();

                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "📍 Xem chi tiết", $"Đang xem: {point ?? "địa điểm"}", "info");
                    }

                    await _context.SaveChangesAsync();

                    // 👈 GỬI CẬP NHẬT DASHBOARD
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
            var activeDevices = await _context.DeviceTracking
                .Where(d => d.IsActive && d.LastActivity >= DateTime.Now.AddMinutes(-5))
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
            var stats = new
            {
                TotalDevices = await _context.DeviceTracking.CountAsync(),
                ActiveDevices = await _context.DeviceTracking
                    .CountAsync(d => d.IsActive && d.LastActivity >= DateTime.Now.AddMinutes(-5)),
                TotalScansToday = await _context.QRScanLogs
                    .CountAsync(s => s.ScanTime >= DateTime.Today),
                TotalListensToday = await _context.TTSLogs
                    .CountAsync(t => t.PlayedAt >= DateTime.Today)
            };

            return Ok(stats);
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
}