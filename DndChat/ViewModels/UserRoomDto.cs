namespace DndChat.ViewModels
{
    public class UserRoomDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string JoinCode { get; set; } = default!;
        public bool IsOwner { get; set; }
    }
}
