using System.ComponentModel.DataAnnotations;

namespace DndChat.ViewModels
{
    public class LoginViewModel
    {
        [Required, MinLength(3), MaxLength(32)]
        public string UserName { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = true;

        // Use LocalRedirect to avoid open redirects.
        public string? ReturnUrl { get; set; }
    }
}
