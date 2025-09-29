using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DndChat.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _log;
        public ChatHub(ILogger<ChatHub> log) => _log = log;

        public override async Task OnConnectedAsync()
        {
            _log.LogInformation("SR connected: {User} ({Conn})", Context.User?.Identity?.Name, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            _log.LogInformation(ex, "SR disconnected: {User} ({Conn})", Context.User?.Identity?.Name, Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }

        public async Task SendMessage(string message)
        {
            var userName = Context.User?.Identity?.Name ?? "unknown";
            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty.");
            _log.LogInformation("Chat: {User} -> {Msg}", userName, message);
            await Clients.All.SendAsync("ReceiveMessage", userName, message.Trim());
        }
    }
}
