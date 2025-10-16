namespace DndChat.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ChatRoomId { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string MessageText { get; set; } = default!;
        public DateTime SentUtc { get; set; } = DateTime.UtcNow;
        public ChatRoom ChatRoom { get; set; } = default!;
    }
}
