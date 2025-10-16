using DndChat.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace DndChat.Hubs
{
    // Only authenticated users may connect (via cookie or JWT).
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class ChatHub : Hub
    {
        // DI: persistence + helpers for rooms/members/messages
        private readonly IChatRoomService _chatRoomService;

        // Constant name for the SignalR "global" group
        public const string GlobalGroup = "global";

        // DI: server-side logging so we see connects/disconnects/errors
        private readonly ILogger<ChatHub> _log;

        public ChatHub(ILogger<ChatHub> log, IChatRoomService chatRoomService)
        {
            _log = log;
            _chatRoomService = chatRoomService;
        }

        // Runs when the SignalR handshake completes.
        // We keep this minimal on purpose: just log the event.
        // The PAGE (global or private room) will explicitly join what it needs.
        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? "unknown";
            _log.LogInformation("SR connected: {User} ({Conn})", userName, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        // Runs when the connection is closed.
        // Also minimal: we just log. We don't broadcast "left chat" here
        // because this connection might have been in a private room only.
        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var userName = Context.User?.Identity?.Name ?? "unknown";
            _log.LogInformation(ex, "SR disconnected: {User} ({Conn})", userName, Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }

        // === GLOBAL CHAT ===

        // Called by the Global page JS AFTER it starts the connection.
        // - Adds THIS connection to the "global" SignalR group so it receives global messages.
        // - Emits a system notice so others in global see that the user joined.
        public async Task JoinGlobal()
        {
            var userName = Context.User?.Identity?.Name ?? "unknown";

            await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroup);

            await Clients.Group(GlobalGroup)
                .SendAsync("SystemNotice", GlobalGroup, $"{userName} joined chat");
        }

        // Optional helper if you ever add a "Leave global" button on the page.
        // (Does NOT remove cookie/JWT auth—this only leaves the SignalR group.)
        public Task LeaveGlobal()
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, GlobalGroup);

        // Sends a message to the global room and (optionally) saves it to DB
        // under the "global" ChatRoom (you seeded this in Program.cs).
        public async Task SendMessage(string message)
        {
            var userId = Context.UserIdentifier ?? "unknown-id";
            var userName = Context.User?.Identity?.Name ?? "unknown";

            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty.");

            if (message.Length > 1000)
                throw new HubException("Message too long.");

            // Persist to DB so global history exists (because you chose Pattern B).
            await _chatRoomService.SaveMessageAsync("global", userId, userName, message);

            // Broadcast to everyone currently in the global SignalR group.
            await Clients.Group(GlobalGroup).SendAsync("ReceiveMessage", userName, message.Trim());
        }

        // === PRIVATE ROOMS (unchanged from your MVP) ===
        // Create, join, send, leave. The private room page calls these explicitly.

        // Creates a private room, adds THIS connection to its SignalR group,
        // and returns the join code the user can share with friends.
        public async Task<(string roomId, string joinCode)> CreatePrivateRoom(string? roomName = null)
        {
            var userId = Context.UserIdentifier ?? throw new HubException("Not authenticated.");
            var userName = Context.User?.Identity?.Name ?? userId;

            var (roomId, joinCode) = await _chatRoomService.CreatePrivateAsync(userId, roomName);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            await Clients.Group(roomId)
                .SendAsync("SystemNotice", roomId, $"{userName} created the room");

            // MVP: joinCode == roomId
            return (roomId, joinCode);
        }

        // Joins a room by its code (same as room id in MVP),
        // ensures membership is saved, sends recent history to the caller,
        // and notifies the room.
        public async Task JoinByCode(string joinCode)
        {
            var userId = Context.UserIdentifier ?? throw new HubException("Not authenticated.");
            var userName = Context.User?.Identity?.Name ?? userId;

            var roomId = await _chatRoomService.ResolveByJoinCodeAsync(joinCode)
                         ?? throw new HubException("Invalid code.");

            if (!await _chatRoomService.IsMemberAsync(userId, roomId))
                await _chatRoomService.AddMemberAsync(userId, roomId);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Load recent history as DTOs so the client gets { UserName, Text, SentUtc }
            var recentDtos = await _chatRoomService.LastMessagesDtoAsync(roomId, 50);
            await Clients.Caller.SendAsync("LoadHistory", roomId, recentDtos);

            await Clients.Group(roomId)
                .SendAsync("SystemNotice", roomId, $"{userName} joined");
        }

        // Sends a message to a specific private room after validating membership.
        public async Task SendRoomMessage(string roomId, string message)
        {
            var userId = Context.UserIdentifier ?? throw new HubException("Not authenticated.");
            var userName = Context.User?.Identity?.Name ?? userId;

            if (string.IsNullOrWhiteSpace(roomId))
                throw new HubException("Room id required.");
            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty.");

            var isMember = await _chatRoomService.IsMemberAsync(userId, roomId);
            if (!isMember) throw new HubException("Not a member.");

            await _chatRoomService.SaveMessageAsync(roomId, userId, userName, message);

            await Clients.Group(roomId)
                .SendAsync("ReceiveRoomMessage", roomId, userName, message.Trim());
        }

        // Leaves the SignalR group for a room; membership in DB remains,
        // so the user will auto-rejoin on next visit/reconnect if you choose.
        public async Task LeaveRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

            await Clients.Group(roomId)
                .SendAsync("SystemNotice", roomId, "Someone left the room");
        }
    }
}
