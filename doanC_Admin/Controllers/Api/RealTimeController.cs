using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using doanC_Admin.Hubs;
using System.Threading.Tasks;

namespace doanC_Admin.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RealTimeController : ControllerBase
    {
        private readonly IHubContext<DashboardHub> _hubContext;

        public RealTimeController(IHubContext<DashboardHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("NotifyNewScan")]
        public async Task<IActionResult> NotifyNewScan([FromBody] ScanNotification notification)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNewData", "QR Scan", notification.Count);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "QR Code mới", $"Có {notification.Count} lượt quét mới!", "success");
            return Ok(new { success = true });
        }

        [HttpPost("NotifyNewLocation")]
        public async Task<IActionResult> NotifyNewLocation([FromBody] LocationNotification notification)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNewData", "Địa điểm mới", 1);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Địa điểm mới", $"Địa điểm '{notification.LocationName}' vừa được thêm!", "info");
            return Ok(new { success = true });
        }

        [HttpPost("RefreshDashboard")]
        public async Task<IActionResult> RefreshDashboard()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate");
            return Ok(new { success = true });
        }

        [HttpPost("UpdateRealtimeStats")]
        public async Task<IActionResult> UpdateRealtimeStats([FromBody] RealtimeStats stats)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveRealtimeStats", stats.ActiveUsers, stats.TodayScans, stats.PendingPOI);
            return Ok(new { success = true });
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