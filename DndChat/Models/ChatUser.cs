namespace DndChat.Models
{
    public class ChatUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; }
        public string ConnectionId { get; set; }
    }
}
