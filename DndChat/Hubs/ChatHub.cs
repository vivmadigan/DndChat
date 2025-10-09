using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace DndChat.Hubs
{
    // This attribute enforces authentication and explicitly lists the two allowed schemes.
    // Identity AND JWT bearer tokens.
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class ChatHub : Hub
    {
        // TEMP a constant group name for your global chat
        // When private chats are added, switch to database-driven groups
        public const string GlobalGroup = "global";
        // logging so we can see connects/disconnects in the console
        private readonly ILogger<ChatHub> _log;
        public ChatHub(ILogger<ChatHub> log) => _log = log;

        // This runs when a client completes the SignalR handshake.
        // Context.User is the authenticated principal built from the cookie or JWT.
        public override async Task OnConnectedAsync()
        {
            // Get the authenticated username (from ClaimTypes.Name)
            var userName = Context.User?.Identity?.Name ?? "unknown";
            // Log it for information purposes
            _log.LogInformation("SR connected: {User} ({Conn})", userName, Context.ConnectionId);
            // Add everyone to the global group
            await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroup);
            // Send a notice to everyone in the group that someone joined
            await Clients.Group(GlobalGroup).SendAsync("SystemNotice", GlobalGroup, $"{userName} joined chat");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            
            var userName = Context.User?.Identity?.Name ?? "unknown";

            _log.LogInformation(ex, "SR disconnected: {User} ({Conn})", userName, Context.ConnectionId);

            await Clients.Group(GlobalGroup).SendAsync("SystemNotice", GlobalGroup, $"{userName} left chat");

            await base.OnDisconnectedAsync(ex);
        }
        // This is a hub method clients can call.
        // It reads the authenticated user's name from Context.User and broadcasts to everyone.
        public async Task SendMessage(string message)
        {
            // Authenticated username (comes from ClaimTypes.Name)
            var userName = Context.User?.Identity?.Name ?? "unknown";
            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty.");
            _log.LogInformation("Chat: {User} -> {Msg}", userName, message);
            await Clients.Group(GlobalGroup).SendAsync("ReceiveMessage", userName, message.Trim());
        }
    }
}
