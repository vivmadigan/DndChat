namespace DndChat.ViewModels
{
    // Small, read-only-ish shape for the room header (id, display name, join code).
    public class RoomHeaderDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string JoinCode { get; set; } = default!;
        public string OwnerUserId { get; set; } = default!;
        public bool IsOwner { get; set; }

    }
}
