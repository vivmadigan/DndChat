namespace DndChat.Models
{
    public class ChatRoom
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JoinCode { get; set; } = default!;
        public string OwnerUserId { get; set; } = default!;
        public string? ChatName { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMembership> Members { get; set; } = new List<ChatMembership>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
