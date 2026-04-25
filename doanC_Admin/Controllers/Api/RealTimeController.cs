using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Hubs;
using doanC_Admin.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class RealTimeController : ControllerBase
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly FoodStreetGuideDBContext _context;
        private readonly ILogger<RealTimeController> _logger;

        public RealTimeController(
            IHubContext<DashboardHub> hubContext,
            FoodStreetGuideDBContext context,
            ILogger<RealTimeController> logger)
        {
            _hubContext = hubContext;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách logs real-time
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int take = 50, [FromQuery] string type = "all")
        {
            try
            {
                var logs = new List<object>();

                // 1. QR Scan logs
                if (type == "all" || type == "qr")
                {
                    var qrLogs = await _context.QRScanLogs
                        .Include(q => q.LocationPoint)
                        .OrderByDescending(q => q.ScanTime)
                        .Take(take)
                        .Select(q => new
                        {
                            Id = q.LogId,
                            Type = "QR_SCAN",
                            Message = q.LocationPoint != null ? $"QR Code quét tại {q.LocationPoint.Name}" : "QR Code quét tại địa điểm không xác định",
                            Location = q.LocationPoint != null ? q.LocationPoint.Name : "Không xác định",
                            LocationId = q.PointId,
                            DeviceId = q.DeviceId,
                            Time = q.ScanTime,
                            Icon = "fa-qrcode",
                            Color = "primary",
                            Details = $"Thiết bị: {q.DeviceId}"
                        })
                        .ToListAsync();

                    logs.AddRange(qrLogs);
                }

                // 2. TTS Listen logs
                if (type == "all" || type == "tts")
                {
                    var ttsLogs = await _context.TTSLogs
                        .Include(t => t.LocationPoint)
                        .OrderByDescending(t => t.PlayedAt)
                        .Take(take)
                        .Select(t => new
                        {
                            Id = t.TtsLogId,
                            Type = "TTS_LISTEN",
                            Message = t.LocationPoint != null ? $"Nghe audio tại {t.LocationPoint.Name} ({(t.DurationSeconds ?? 0)} giây)" : $"Nghe audio ({(t.DurationSeconds ?? 0)} giây)",
                            Location = t.LocationPoint != null ? t.LocationPoint.Name : "Không xác định",
                            LocationId = t.PointId,
                            DeviceId = "Mobile App",
                            Time = t.PlayedAt,
                            Icon = "fa-headphones",
                            Color = "success",
                            Details = $"Thời gian nghe: {t.DurationSeconds ?? 0}s, LanguageId: {t.LanguageId}"
                        })
                        .ToListAsync();

                    logs.AddRange(ttsLogs);
                }

                // 3. GeoFence logs
                if (type == "all" || type == "geofence")
                {
                    var geoLogs = await _context.GeoFenceLogs
                        .Include(g => g.LocationPoint)
                        .OrderByDescending(g => g.EnterTime)
                        .Take(take)
                        .Select(g => new
                        {
                            Id = g.GeoLogId,
                            Type = "GEOFENCE",
                            Message = g.LocationPoint != null ? $"Di chuyển vào khu vực {g.LocationPoint.Name}" : "Di chuyển vào khu vực không xác định",
                            Location = g.LocationPoint != null ? g.LocationPoint.Name : "Không xác định",
                            LocationId = g.PointId,
                            DeviceId = g.DeviceId ?? "Unknown",
                            Time = g.EnterTime,
                            Icon = "fa-location-dot",
                            Color = "info",
                            Details = (g.ExitTime.HasValue && g.EnterTime.HasValue) ?
                                $"Thời gian ở lại: {(g.ExitTime.Value - g.EnterTime.Value).TotalMinutes.ToString("F0")} phút" :
                                "Đang ở trong khu vực"
                        })
                        .ToListAsync();

                    logs.AddRange(geoLogs);
                }

                // 4. New location registrations
                if (type == "all" || type == "location")
                {
                    var locationLogs = await _context.LocationPoints
                        .Where(l => l.CreatedAt >= DateTime.Now.AddDays(-7))
                        .OrderByDescending(l => l.CreatedAt)
                        .Take(take)
                        .Select(l => new
                        {
                            Id = l.PointId,
                            Type = "NEW_LOCATION",
                            Message = $"Địa điểm mới: {l.Name}",
                            Location = l.Name,
                            LocationId = l.PointId,
                            DeviceId = l.OwnerId != null ? l.OwnerId.ToString() : "Unknown",
                            Time = l.CreatedAt,
                            Icon = "fa-map-marker-alt",
                            Color = l.IsApproved ? "success" : "warning",
                            Details = l.IsApproved ? "Đã duyệt" : "Chờ duyệt",
                            IsPending = !l.IsApproved
                        })
                        .ToListAsync();

                    logs.AddRange(locationLogs);
                }

                // Sắp xếp theo thời gian giảm dần
                var allLogs = logs
                    .OrderByDescending(l => GetTimeFromLog(l))
                    .Take(take)
                    .ToList();

                return Ok(new { success = true, data = allLogs, total = allLogs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting logs: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Helper method để lấy thời gian từ log object
        private DateTime GetTimeFromLog(object log)
        {
            var property = log.GetType().GetProperty("Time");
            if (property != null)
            {
                return (DateTime)property.GetValue(log);
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// Lấy thống kê real-time
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetRealtimeStats()
        {
            try
            {
                var today = DateTime.Today;
                var lastHour = DateTime.Now.AddHours(-1);
                var last30Minutes = DateTime.Now.AddMinutes(-30);

                var stats = new
                {
                    TotalLocations = await _context.LocationPoints.CountAsync(),
                    PendingLocations = await _context.LocationPoints.CountAsync(l => !l.IsApproved),
                    ApprovedLocations = await _context.LocationPoints.CountAsync(l => l.IsApproved),
                    TotalQRScans = await _context.QRScanLogs.CountAsync(),
                    TodayQRScans = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= today),
                    ScansLastHour = await _context.QRScanLogs.CountAsync(s => s.ScanTime >= lastHour),
                    TotalTTSListens = await _context.TTSLogs.CountAsync(),
                    TodayTTSListens = await _context.TTSLogs.CountAsync(t => t.PlayedAt >= today),
                    ListensLastHour = await _context.TTSLogs.CountAsync(t => t.PlayedAt >= lastHour),
                    TotalGeofenceEntries = await _context.GeoFenceLogs.CountAsync(),
                    TodayGeofenceEntries = await _context.GeoFenceLogs.CountAsync(g => g.EnterTime >= today),
                    ActiveDevices = await _context.QRScanLogs
                        .Where(s => s.ScanTime >= last30Minutes)
                        .Select(s => s.DeviceId)
                        .Distinct()
                        .CountAsync(),
                    UpdatedAt = DateTime.Now
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting stats: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê theo giờ (cho biểu đồ)
        /// </summary>
        [HttpGet("hourly-stats")]
        public async Task<IActionResult> GetHourlyStats([FromQuery] int hours = 24)
        {
            try
            {
                var hourlyStats = new List<object>();
                var now = DateTime.Now;

                for (int i = hours - 1; i >= 0; i--)
                {
                    var hourStart = now.AddHours(-i);
                    var hourEnd = hourStart.AddHours(1);

                    var scans = await _context.QRScanLogs
                        .CountAsync(s => s.ScanTime >= hourStart && s.ScanTime < hourEnd);

                    var listens = await _context.TTSLogs
                        .CountAsync(t => t.PlayedAt >= hourStart && t.PlayedAt < hourEnd);

                    var geofences = await _context.GeoFenceLogs
                        .CountAsync(g => g.EnterTime >= hourStart && g.EnterTime < hourEnd);

                    hourlyStats.Add(new
                    {
                        Hour = hourStart.ToString("dd/MM HH:00"),
                        Scans = scans,
                        Listens = listens,
                        Geofences = geofences,
                        Total = scans + listens + geofences
                    });
                }

                return Ok(new { success = true, data = hourlyStats });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting hourly stats: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ========== POST ENDPOINTS ==========

        [HttpPost("NotifyNewScan")]
        public async Task<IActionResult> NotifyNewScan([FromBody] ScanNotification notification)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNewData", "QR Scan", notification.Count);
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "QR Code mới", $"Có {notification.Count} lượt quét mới!", "success");
                await _hubContext.Clients.All.SendAsync("RefreshDashboard");

                _logger.LogInformation($"Notified {notification.Count} new scans");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying new scan: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("NotifyNewLocation")]
        public async Task<IActionResult> NotifyNewLocation([FromBody] LocationNotification notification)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNewData", "Địa điểm mới", 1);
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Địa điểm mới", $"Địa điểm '{notification.LocationName}' vừa được thêm!", "info");
                await _hubContext.Clients.All.SendAsync("RefreshDashboard");

                _logger.LogInformation($"Notified new location: {notification.LocationName}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying new location: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("RefreshDashboard")]
        public async Task<IActionResult> RefreshDashboard()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate");
                await _hubContext.Clients.All.SendAsync("RefreshDashboard");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing dashboard: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("UpdateRealtimeStats")]
        public async Task<IActionResult> UpdateRealtimeStats([FromBody] RealtimeStats stats)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveRealtimeStats", stats.ActiveUsers, stats.TodayScans, stats.PendingPOI);
                await _hubContext.Clients.All.SendAsync("UpdateRealtimeStats", stats);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating stats: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        // ========== API NHẬN SỰ KIỆN TỪ MAUI APP ==========

        [HttpPost("RecordQRScan")]
        public async Task<IActionResult> RecordQRScan([FromBody] QRScanRecord request)
        {
            try
            {
                // Lưu vào database
                var scanLog = new QRScanLog
                {
                    PointId = request.PointId,
                    DeviceId = request.DeviceId,
                    ScanTime = DateTime.Now,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude
                };
                _context.QRScanLogs.Add(scanLog);
                await _context.SaveChangesAsync();

                // Lấy tên địa điểm
                var location = await _context.LocationPoints
                    .Where(l => l.PointId == request.PointId)
                    .Select(l => l.Name)
                    .FirstOrDefaultAsync();

                // Gửi thông báo real-time
                await _hubContext.Clients.All.SendAsync("NewQRScan", location ?? "Địa điểm", request.DeviceId);
                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    "QR Code mới",
                    $"🔔 Lượt quét mới tại: {location ?? "Địa điểm"}",
                    "info");
                await _hubContext.Clients.All.SendAsync("RefreshDashboard");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error recording QR scan: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("RecordTTSListen")]
        public async Task<IActionResult> RecordTTSListen([FromBody] TTSListenRecord request)
        {
            try
            {
                // ✅ SỬA: Thêm DeviceId khi lưu
                var ttsLog = new TTSLog
                {
                    PointId = request.PointId,
                    LanguageId = request.LanguageId,
                    PlayedAt = DateTime.Now,
                    DurationSeconds = request.DurationSeconds,
                    DeviceId = request.DeviceId ?? "Unknown" 
                };
                _context.TTSLogs.Add(ttsLog);
                await _context.SaveChangesAsync();

                // Lấy tên địa điểm
                var location = await _context.LocationPoints
                    .Where(l => l.PointId == request.PointId)
                    .Select(l => l.Name)
                    .FirstOrDefaultAsync();

                // Gửi thông báo real-time
                await _hubContext.Clients.All.SendAsync("NewTTSListen",
                    location ?? "Địa điểm",
                    request.DurationSeconds,
                    request.DeviceId ?? "MobileApp");
                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    "Audio Guide mới",
                    $"🎧 Có lượt nghe thuyết minh tại: {location ?? "Địa điểm"}",
                    "info");
                await _hubContext.Clients.All.SendAsync("RefreshDashboard");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error recording TTS listen: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Request/Response classes
        public class QRScanRecord
        {
            public int PointId { get; set; }
            public string DeviceId { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        public class TTSListenRecord
        {
            public int PointId { get; set; }
            public int LanguageId { get; set; } = 1;
            public int DurationSeconds { get; set; }
            public string? DeviceId { get; set; }
        }

        // ========== TEST ENDPOINTS ==========

        [HttpPost("NotifyNewListen")]
        public async Task<IActionResult> NotifyNewListen([FromBody] ListenNotification notification)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("NewTTSListen",
                    notification.LocationName,
                    notification.DurationSeconds,
                    notification.DeviceId ?? "TestDevice");

                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    "Audio Guide mới",
                    $"🎧 Test: {notification.LocationName} ({notification.DurationSeconds}s)",
                    "info");

                _logger.LogInformation($"Test TTS notification sent: {notification.LocationName}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("NotifyNewQRScan")]
        public async Task<IActionResult> NotifyNewQRScan([FromBody] QRScanNotification notification)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("NewQRScan",
                    notification.LocationName,
                    notification.DeviceId ?? "TestDevice");

                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                    "QR Code mới",
                    $"🔔 Test: {notification.LocationName}",
                    "info");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }   
        // Trong RealTimeController.cs - Thêm API lấy top locations theo avg time

        [HttpGet("top-locations")]
        public async Task<IActionResult> GetTopLocations([FromQuery] string metric = "listens", [FromQuery] int take = 5)
        {
            try
            {
                if (metric == "listens")
                {
                    // Top địa điểm được nghe nhiều nhất
                    var topLocations = await _context.TTSLogs
                        .Include(t => t.LocationPoint)
                        .Where(t => t.LocationPoint != null)
                        .GroupBy(t => t.PointId)
                        .Select(g => new
                        {
                            PointId = g.Key,
                            LocationName = g.First().LocationPoint.Name,
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .Take(take)
                        .ToListAsync();

                    return Ok(new { success = true, data = topLocations, metric = "listens" });
                }
                else if (metric == "avgtime")
                {
                    // Top địa điểm có thời gian nghe trung bình cao nhất
                    var topAvgTime = await _context.TTSLogs
                        .Include(t => t.LocationPoint)
                        .Where(t => t.LocationPoint != null && t.DurationSeconds.HasValue)
                        .GroupBy(t => t.PointId)
                        .Select(g => new
                        {
                            PointId = g.Key,
                            LocationName = g.First().LocationPoint.Name,
                            AvgTime = g.Average(t => t.DurationSeconds.Value)
                        })
                        .OrderByDescending(x => x.AvgTime)
                        .Take(take)
                        .ToListAsync();

                    return Ok(new { success = true, data = topAvgTime, metric = "avgtime" });
                }

                return BadRequest(new { success = false, message = "Invalid metric" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy dữ liệu di chuyển 7 ngày qua cho biểu đồ (real-time refresh)
        /// </summary>
        [HttpGet("movement-chart")]
        public async Task<IActionResult> GetMovementChart()
        {
            try
            {
                var labels = new List<string>();
                var counts = new List<int>();

                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Now.AddDays(-i).Date;
                    labels.Add(date.ToString("dd/MM"));
                    var count = await _context.GeoFenceLogs
                        .Where(g => g.EnterTime.HasValue && g.EnterTime.Value.Date == date)
                        .CountAsync();
                    counts.Add(count);
                }

                var data = labels.Select((label, idx) => new { date = label, count = counts[idx] }).ToList();

                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting movement chart: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // ========== DTO CLASSES ==========
    public class ScanNotification
    {
        public int Count { get; set; }
        public int PointId { get; set; }
    }

    public class LocationNotification
    {
        public string LocationName { get; set; } = string.Empty;
        public int PointId { get; set; }
    }

    public class RealtimeStats
    {
        public int ActiveUsers { get; set; }
        public int TodayScans { get; set; }
        public int PendingPOI { get; set; }
    }

    public class ListenNotification
    {
        public string LocationName { get; set; } = string.Empty;
        public int DurationSeconds { get; set; } = 45;
        public string? DeviceId { get; set; }
    }

    public class QRScanNotification
    {
        public string LocationName { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
    }
}