using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace doanC_Admin.Hubs
{
    public class DashboardHub : Hub
    {
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

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}