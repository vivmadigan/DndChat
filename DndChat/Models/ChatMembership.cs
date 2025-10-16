namespace DndChat.Models
{
    public class ChatMembership
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ChatRoomId { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
        public ChatRoom ChatRoom { get; set; } = default!;
        
    }
}
