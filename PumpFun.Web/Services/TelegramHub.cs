using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace PumpFun.Web.Hubs
{
    public class TelegramHub : Hub
    {
        private readonly ILogger<TelegramHub> _logger;

        public TelegramHub(ILogger<TelegramHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Client connected: {ConnectionId}", connectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Client disconnected: {ConnectionId}", connectionId);
            
            if (exception != null)
            {
                _logger.LogError(exception, "Client disconnected with error: {ConnectionId}", connectionId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined group: {GroupName}", 
                Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left group: {GroupName}", 
                Context.ConnectionId, groupName);
        }
    }
}