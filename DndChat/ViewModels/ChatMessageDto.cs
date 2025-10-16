namespace DndChat.ViewModels
{
    // Small data object we send to the client for chat history/live messages.
    public class ChatMessageDto
    {
        public string UserName { get; set; } = default!;
        public string Text { get; set; } = default!;
        public DateTime SentUtc { get; set; }
    }
}
