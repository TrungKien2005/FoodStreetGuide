using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using doanC_Admin.Models;
using doanC_Admin.Hubs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace doanC_Admin.Services
{
 public class DeviceCleanupService : BackgroundService
 {
 private readonly IServiceProvider _services;
 private readonly ILogger<DeviceCleanupService> _logger;
 private readonly IHubContext<DashboardHub> _hubContext;
 // Run cleanup frequently and mark devices inactive shortly after missing heartbeats
 private TimeSpan _interval = TimeSpan.FromSeconds(10); // check every10s
 private TimeSpan _timeout = TimeSpan.FromSeconds(15); // mark inactive if LastActivity older than15s

 public DeviceCleanupService(IServiceProvider services, ILogger<DeviceCleanupService> logger, IHubContext<DashboardHub> hubContext)
 {
 _services = services;
 _logger = logger;
 _hubContext = hubContext;
 }

 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
 _logger.LogInformation("DeviceCleanupService started.");
 while (!stoppingToken.IsCancellationRequested)
 {
 try
 {
 using var scope = _services.CreateScope();
 var db = scope.ServiceProvider.GetRequiredService<FoodStreetGuideDBContext>();

 var cutoff = DateTime.Now.Subtract(_timeout);
 var staleDevices = await db.DeviceTracking
 .Where(d => d.IsActive && d.LastActivity < cutoff)
 .ToListAsync(stoppingToken);

 if (staleDevices.Any())
 {
 foreach (var d in staleDevices)
 {
 d.IsActive = false;
 }

 await db.SaveChangesAsync(stoppingToken);
 _logger.LogInformation("Marked {count} devices offline by cleanup.", staleDevices.Count);

 // Notify admin UI
 try
 {
 await _hubContext.Clients.All.SendAsync("RefreshDeviceList", cancellationToken: stoppingToken);
 await _hubContext.Clients.All.SendAsync("RefreshStats", cancellationToken: stoppingToken);
 }
 catch (Exception ex)
 {
 _logger.LogWarning(ex, "Error notifying hub after cleanup");
 }
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error in DeviceCleanupService loop.");
 }

 try
 {
 await Task.Delay(_interval, stoppingToken);
 }
 catch (TaskCanceledException) { }
 }
 }
 }
}
