using DndChat.Data;
using DndChat.Models;
using Microsoft.EntityFrameworkCore;
using DndChat.ViewModels;

namespace DndChat.Services
{
    // Provides room creation/lookup, membership, and message persistence for the chat hub.
    public class ChatRoomService : IChatRoomService
    {
        private readonly ApplicationDbContext _dbContext;
        public ChatRoomService(ApplicationDbContext dbContext) => _dbContext = dbContext;

        // Creates a private room using a canonical join code (lowercase, no dashes),
        // saves the creator as a member, and returns both the room id and the code to share.
        public async Task<(string roomId, string joinCode)> CreatePrivateAsync(string ownerUserId, string? name = null)
        {
            var roomId = Guid.NewGuid().ToString("N"); // canonical form
            var newRoom = new ChatRoom
            {
                Id = roomId,                  // set the Id explicitly (overrides property initializer)
                JoinCode = roomId,            // store the same canonical value as the join code
                OwnerUserId = ownerUserId,
                ChatName = name
            };

            _dbContext.ChatRooms.Add(newRoom);
            _dbContext.ChatMemberships.Add(new ChatMembership { ChatRoomId = roomId, UserId = ownerUserId });
            await _dbContext.SaveChangesAsync();
            return (roomId, roomId);         // MVP: code == roomId
        }

        // Looks up a room from what the user typed. We normalize the input so that
        // UPPER/lower case and optional dashes still match the stored canonical code.
        public async Task<string?> ResolveByJoinCodeAsync(string joinCode)
        {
            var normalized = (joinCode ?? string.Empty)
                .Trim()
                .Replace("-", "")
                .ToLowerInvariant();

            return await _dbContext.ChatRooms
                .Where(room => room.JoinCode == normalized)
                .Select(room => room.Id)
                .FirstOrDefaultAsync();
        }

        // Returns true if the user is already in this room (enforced by unique index too).
        public Task<bool> IsMemberAsync(string userId, string roomId) =>
            _dbContext.ChatMemberships.AnyAsync(m => m.ChatRoomId == roomId && m.UserId == userId);

        // Adds the user to the room if not already present.
        public async Task AddMemberAsync(string userId, string roomId)
        {
            if (!await IsMemberAsync(userId, roomId))
            {
                _dbContext.ChatMemberships.Add(new ChatMembership { ChatRoomId = roomId, UserId = userId });
                await _dbContext.SaveChangesAsync();
            }
        }

        // Lists all room ids the user belongs to (used on connect to rejoin SignalR groups).
        public async Task<List<string>> RoomsForUserAsync(string userId) =>
            await _dbContext.ChatMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.ChatRoomId)
                .ToListAsync();

        // Persists a message for room history.
        public async Task SaveMessageAsync(string roomId, string userId, string userName, string text)
        {
            var message = new ChatMessage
            {
                ChatRoomId = roomId,
                UserId = userId,
                UserName = userName,
                MessageText = text.Trim()
            };
            _dbContext.ChatMessages.Add(message);
            await _dbContext.SaveChangesAsync();
        }

        // Gets the most recent N messages, ordered oldest→newest for easy rendering.
        public async Task<List<(string UserName, string Text, DateTime SentUtc)>> LastMessagesAsync(string roomId, int take = 50)
        {
            return await _dbContext.ChatMessages
                .Where(m => m.ChatRoomId == roomId)
                .OrderByDescending(m => m.SentUtc)
                .Take(take)
                .OrderBy(m => m.SentUtc)
                .Select(m => new ValueTuple<string, string, DateTime>(m.UserName, m.MessageText, m.SentUtc))
                .ToListAsync();
        }

        // Returns minimal header data for the room page.
        public async Task<RoomHeaderDto?> GetRoomHeaderAsync(string roomId)
        {
            return await _dbContext.ChatRooms
                .AsNoTracking()
                .Where(r => r.Id == roomId)
                .Select(r => new RoomHeaderDto
                {
                    Id = r.Id,
                    Name = r.ChatName ?? "Private Room",
                    JoinCode = r.JoinCode
                })
                .SingleOrDefaultAsync();
        }

        // Secure: only return if the user belongs to the room (global is always allowed).
        public async Task<RoomHeaderDto?> GetRoomHeaderForUserAsync(string userId, string roomId)
        {
            // Block access if not a member (global always allowed)
            if (!string.Equals(roomId, "global", StringComparison.OrdinalIgnoreCase))
            {
                var isMember = await _dbContext.ChatMemberships
                    .AsNoTracking()
                    .AnyAsync(m => m.ChatRoomId == roomId && m.UserId == userId);
                if (!isMember) return null;
            }

            // Project to header and compute IsOwner
            return await _dbContext.ChatRooms
                .AsNoTracking()
                .Where(r => r.Id == roomId)
                .Select(r => new RoomHeaderDto
                {
                    Id = r.Id,
                    Name = r.ChatName ?? "Private Room",
                    JoinCode = r.JoinCode,
                    OwnerUserId = r.OwnerUserId,
                    IsOwner = (r.OwnerUserId == userId) // <-- compute here
                })
                .SingleOrDefaultAsync();
        }

        // History projected to DTOs so the client gets a clean shape.
        public async Task<List<ChatMessageDto>> LastMessagesDtoAsync(string roomId, int take = 50)
        {
            return await _dbContext.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatRoomId == roomId)
                .OrderByDescending(m => m.SentUtc)
                .Take(take)
                .OrderBy(m => m.SentUtc) // oldest → newest for rendering
                .Select(m => new ChatMessageDto
                {
                    UserName = m.UserName,
                    Text = m.MessageText,
                    SentUtc = m.SentUtc
                })
                .ToListAsync();
        }
        // Returns rooms (except the "global" row) that the user belongs to.
        public async Task<IReadOnlyList<UserRoomDto>> RoomsForUserListAsync(string userId)
        {
            return await _dbContext.ChatMemberships
                .AsNoTracking()
                .Where(m => m.UserId == userId && m.ChatRoomId != "global")
                .Select(m => new UserRoomDto
                {
                    Id = m.ChatRoomId,
                    Name = m.ChatRoom.ChatName ?? "Private Room",
                    JoinCode = m.ChatRoom.JoinCode,
                    IsOwner = m.ChatRoom.OwnerUserId == userId
                })
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        // Removes a single membership (does not delete messages or the room itself).
        public async Task RemoveMemberAsync(string userId, string roomId)
        {
            var membership = await _dbContext.ChatMemberships
                .Where(m => m.UserId == userId && m.ChatRoomId == roomId)
                .FirstOrDefaultAsync();

            if (membership is null) return;

            _dbContext.ChatMemberships.Remove(membership);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsOwnerAsync(string userId, string roomId)
        {
            return await _dbContext.ChatRooms
                .AsNoTracking()
                .AnyAsync(r => r.Id == roomId && r.OwnerUserId == userId);
        }

        public async Task<bool> DeleteRoomAsync(string ownerUserId, string roomId)
        {
            // Never allow deleting the seeded global room
            if (string.Equals(roomId, "global", StringComparison.OrdinalIgnoreCase))
                return false;

            // Make sure the caller owns this room
            var room = await _dbContext.ChatRooms
                .FirstOrDefaultAsync(r => r.Id == roomId && r.OwnerUserId == ownerUserId);

            if (room is null) return false;

            // Cascade deletes will remove memberships + messages (you already configured Cascade)
            _dbContext.ChatRooms.Remove(room);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}

