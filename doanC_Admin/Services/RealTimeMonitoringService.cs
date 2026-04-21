// Services/RealTimeMonitoringService.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using doanC_Admin.Hubs;
using doanC_Admin.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace doanC_Admin.Services
{
    public class RealTimeMonitoringService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<RealTimeMonitoringService> _logger;
        private System.Timers.Timer _timer;
        private bool _isRunning = false;

        // Cache để so sánh thay đổi
        private int _lastPendingCount = 0;
        private int _lastQRScanCount = 0;
        private int _lastTTSListenCount = 0;
        private DateTime _lastCheckTime;

        public RealTimeMonitoringService(
            IServiceScopeFactory scopeFactory,
            IHubContext<DashboardHub> hubContext,
            ILogger<RealTimeMonitoringService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 RealTimeMonitoringService started at: {time}", DateTime.Now);
            _lastCheckTime = DateTime.Now;

            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += async (sender, e) => await CheckForChanges();
            _timer.Start();

            await Task.CompletedTask;
        }

        private async Task CheckForChanges()
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<FoodStreetGuideDBContext>();

                if (!await context.Database.CanConnectAsync())
                {
                    _logger.LogWarning("Cannot connect to database");
                    return;
                }

                await CheckNewPendingLocations(context);
                await CheckNewQRScans(context);
                await CheckNewTTSListens(context);

                _lastCheckTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RealTimeMonitoringService.CheckForChanges");
            }
            finally
            {
                _isRunning = false;
            }
        }

        private async Task CheckNewPendingLocations(FoodStreetGuideDBContext context)
        {
            try
            {
                var currentPendingCount = await context.LocationPoints.CountAsync(l => l.IsApproved == false);

                if (currentPendingCount > _lastPendingCount)
                {
                    var newLocations = await context.LocationPoints
                        .Where(l => l.IsApproved == false && l.CreatedAt > _lastCheckTime)
                        .Include(l => l.Owner)
                        .ThenInclude(o => o.AdminUser)
                        .ToListAsync();

                    foreach (var location in newLocations)
                    {
                        string ownerName = "Chủ quán";
                        if (location.Owner?.AdminUser != null)
                        {
                            ownerName = location.Owner.AdminUser.Username ?? location.Owner.AdminUser.FullName ?? "Chủ quán";
                        }

                        _logger.LogInformation("📍 New pending location: {LocationName} by {OwnerName} (ID: {PointId})",
                            location.Name, ownerName, location.PointId);

                        await _hubContext.Clients.All.SendAsync("NewPendingPoi",
                            location.Name, ownerName);

                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "🏪 Địa điểm mới cần duyệt",
                            $"{ownerName} vừa đăng ký địa điểm: {location.Name}",
                            "warning");

                        await _hubContext.Clients.All.SendAsync("RefreshDashboard");
                    }

                    _lastPendingCount = currentPendingCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking new pending locations");
            }
        }

        private async Task CheckNewQRScans(FoodStreetGuideDBContext context)
        {
            try
            {
                var currentQRScanCount = await context.QRScanLogs
                    .CountAsync(s => s.ScanTime > _lastCheckTime);

                if (currentQRScanCount > 0)
                {
                    var newScans = await context.QRScanLogs
                        .Where(s => s.ScanTime > _lastCheckTime)
                        .Include(s => s.LocationPoint)
                        .OrderByDescending(s => s.ScanTime)
                        .Take(10)
                        .ToListAsync();

                    foreach (var scan in newScans)
                    {
                        var locationName = scan.LocationPoint?.Name ?? "Địa điểm không xác định";

                        _logger.LogInformation("🔔 New QR scan at {LocationName} from device {DeviceId}",
                            locationName, scan.DeviceId);

                        await _hubContext.Clients.All.SendAsync("NewQRScan", locationName, scan.DeviceId);

                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "QR Code mới",
                            $"🔔 Lượt quét mới tại: {locationName}",
                            "info");
                    }

                    await UpdateRealtimeStats(context);
                    await _hubContext.Clients.All.SendAsync("RefreshDashboard");

                    _lastQRScanCount = currentQRScanCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking new QR scans");
            }
        }

        private async Task CheckNewTTSListens(FoodStreetGuideDBContext context)
        {
            try
            {
                var currentTTSListenCount = await context.TTSLogs
                    .CountAsync(t => t.PlayedAt > _lastCheckTime);

                if (currentTTSListenCount > 0)
                {
                    var newListens = await context.TTSLogs
                        .Where(t => t.PlayedAt > _lastCheckTime)
                        .Include(t => t.LocationPoint)
                        .OrderByDescending(t => t.PlayedAt)
                        .Take(10)
                        .ToListAsync();

                    foreach (var listen in newListens)
                    {
                        var locationName = listen.LocationPoint?.Name ?? "Địa điểm không xác định";
                        var duration = listen.DurationSeconds ?? 0;

                        _logger.LogInformation("🎧 New TTS listen at {LocationName} - {Duration}s", locationName, duration);

                        await _hubContext.Clients.All.SendAsync("NewTTSListen", locationName, duration, "MobileApp");

                        await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                            "Audio Guide mới",
                            $"🎧 Có lượt nghe thuyết minh tại: {locationName}",
                            "info");
                    }

                    await UpdateRealtimeStats(context);
                    await _hubContext.Clients.All.SendAsync("RefreshDashboard");

                    _lastTTSListenCount = currentTTSListenCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking new TTS listens");
            }
        }

        private async Task UpdateRealtimeStats(FoodStreetGuideDBContext context)
        {
            try
            {
                var today = DateTime.Today;
                var lastHour = DateTime.Now.AddHours(-1);
                var last30Minutes = DateTime.Now.AddMinutes(-30);

                var stats = new
                {
                    TotalLocations = await context.LocationPoints.CountAsync(),
                    PendingLocations = await context.LocationPoints.CountAsync(l => !l.IsApproved),
                    TodayScans = await context.QRScanLogs.CountAsync(s => s.ScanTime >= today),
                    ScansLastHour = await context.QRScanLogs.CountAsync(s => s.ScanTime >= lastHour),
                    TodayListens = await context.TTSLogs.CountAsync(t => t.PlayedAt >= today),
                    ActiveDevices = await context.QRScanLogs
                        .Where(s => s.ScanTime >= last30Minutes)
                        .Select(s => s.DeviceId)
                        .Distinct()
                        .CountAsync(),
                    UpdatedAt = DateTime.Now
                };

                await _hubContext.Clients.All.SendAsync("UpdateRealtimeStats", stats);
                _logger.LogDebug("Updated realtime stats at {time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating realtime stats");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 RealTimeMonitoringService stopping at: {time}", DateTime.Now);
            _timer?.Stop();
            _timer?.Dispose();
            await base.StopAsync(stoppingToken);
        }
    }
}