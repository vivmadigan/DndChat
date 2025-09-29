using System.ComponentModel.DataAnnotations;

namespace DndChat.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MinLength(3), MaxLength(32)]
        public string UserName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(2), MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required, MinLength(2), MaxLength(100)]
        public string LastName { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }
    }
}
