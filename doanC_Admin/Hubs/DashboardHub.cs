using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace doanC_Admin.Hubs
{
    public class DashboardHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _connectedUsers = new();

        public async Task SendDashboardUpdate()
        {
            await Clients.All.SendAsync("ReceiveDashboardUpdate");
        }

        public async Task NotifyNewData(string dataType, int count)
        {
            await Clients.All.SendAsync("ReceiveNewData", dataType, count);
        }

        public async Task SendNotification(string title, string message, string type = "info")
        {
            await Clients.All.SendAsync("ReceiveNotification", title, message, type);
        }

        public async Task RefreshDeviceList()
        {
            await Clients.All.SendAsync("RefreshDeviceList");
        }

        public async Task RefreshStats()
        {
            await Clients.All.SendAsync("RefreshStats");
        }

        public async Task RefreshDashboard()
        {
            await Clients.All.SendAsync("RefreshDashboard");
        }

        public async Task NotifyNewPendingPoi(string poiName, string ownerName)
        {
            await Clients.All.SendAsync("NewPendingPoi", poiName, ownerName);
        }

        public async Task NotifyNewQRScan(string locationName, string deviceId)
        {
            await Clients.All.SendAsync("NewQRScan", locationName, deviceId);
        }

        public async Task UpdateRealtimeStats(object stats)
        {
            await Clients.All.SendAsync("UpdateRealtimeStats", stats);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            _connectedUsers.TryAdd(Context.ConnectionId, userId);

            await Clients.All.SendAsync("UserConnected", userId);
            await Clients.All.SendAsync("UpdateOnlineCount", _connectedUsers.Count);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _connectedUsers.TryRemove(Context.ConnectionId, out _);
            await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
            await Clients.All.SendAsync("UpdateOnlineCount", _connectedUsers.Count);
            await base.OnDisconnectedAsync(exception);
        }

        public int GetOnlineUsersCount()
        {
            return _connectedUsers.Count;
        }
    }
}