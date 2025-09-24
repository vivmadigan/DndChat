using System.ComponentModel.DataAnnotations;

namespace DndChat.ViewModels
{
    public class JoinViewModel
    {
        [Required, MinLength(3), MaxLength(32)]
        public string Username { get; set; } = "";
        public bool Joined { get; set; }  
    }
}
