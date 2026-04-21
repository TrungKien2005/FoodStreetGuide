using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using doanC_Admin.Hubs;
using doanC_Admin.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;

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
                            Id = q.LogId,  // Dùng LogId (theo model QRScanLog)
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

        /// <summary>
        /// Lấy top địa điểm hoạt động nhiều nhất
        /// </summary>
        [HttpGet("top-locations")]
        public async Task<IActionResult> GetTopLocations([FromQuery] int take = 5, [FromQuery] string metric = "scans")
        {
            try
            {
                if (metric == "scans")
                {
                    var topLocations = await _context.QRScanLogs
                        .Include(q => q.LocationPoint)
                        .Where(q => q.LocationPoint != null)
                        .GroupBy(q => q.PointId)
                        .Select(g => new
                        {
                            PointId = g.Key,
                            LocationName = g.First().LocationPoint != null ? g.First().LocationPoint.Name : "Không xác định",
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .Take(take)
                        .ToListAsync();

                    return Ok(new { success = true, data = topLocations, metric = "scans" });
                }
                else if (metric == "listens")
                {
                    var topLocations = await _context.TTSLogs
                        .Include(t => t.LocationPoint)
                        .Where(t => t.LocationPoint != null)
                        .GroupBy(t => t.PointId)
                        .Select(g => new
                        {
                            PointId = g.Key,
                            LocationName = g.First().LocationPoint != null ? g.First().LocationPoint.Name : "Không xác định",
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .Take(take)
                        .ToListAsync();

                    return Ok(new { success = true, data = topLocations, metric = "listens" });
                }

                return BadRequest(new { success = false, message = "Invalid metric. Use 'scans' or 'listens'" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting top locations: {ex.Message}");
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
    }

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
}