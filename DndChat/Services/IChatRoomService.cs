using DndChat.ViewModels;

namespace DndChat.Services
{
    public interface IChatRoomService
    {
        Task<(string roomId, string joinCode)> CreatePrivateAsync(string ownerUserId, string? name = null);
        Task<string?> ResolveByJoinCodeAsync(string joinCode);
        Task<bool> IsMemberAsync(string userId, string roomId);
        Task AddMemberAsync(string userId, string roomId);
        Task<List<string>> RoomsForUserAsync(string userId);
        Task SaveMessageAsync(string roomId, string userId, string userName, string text);
        Task<List<(string UserName, string Text, DateTime SentUtc)>> LastMessagesAsync(string roomId, int take = 50);

        // NEW: read-only room header for the controller (id + name + code)
        Task<RoomHeaderDto?> GetRoomHeaderAsync(string roomId);

        Task<RoomHeaderDto?> GetRoomHeaderForUserAsync(string userId, string roomId);

        // Returns message history already projected to a simple DTO shape that the JS expects.
        // Using DTOs avoids tuple serialization quirks and keeps the payload stable.
        Task<List<ChatMessageDto>> LastMessagesDtoAsync(string roomId, int take = 50);

        // Lists all private rooms this user belongs to (excluding "global")
        Task<IReadOnlyList<UserRoomDto>> RoomsForUserListAsync(string userId);

        // Optional: allow a user to leave a room (keeps the room for others)
        Task RemoveMemberAsync(string userId, string roomId);

        Task<bool> DeleteRoomAsync(string ownerUserId, string roomId);
        Task<bool> IsOwnerAsync(string userId, string roomId);
    }
}
