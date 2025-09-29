using DndChat.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DndChat.Services
{
    public class UserService(UserManager<ChatUser> userManager, SignInManager<ChatUser> signInManager)
    {
        private readonly UserManager<ChatUser> _userManager = userManager;
        private readonly SignInManager<ChatUser> _signInManager = signInManager;

        public async Task<(bool ok, string error)> RegisterAsync(
       string userName, string email, string firstName, string lastName, string password)
        {
            var user = new ChatUser
            {
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));

            // optional: auto sign in after register
            await _signInManager.SignInAsync(user, isPersistent: false);
            return (true, "");
        }

        public Task<SignInResult> LoginAsync(string userName, string password, bool rememberMe = true) =>
            _signInManager.PasswordSignInAsync(userName, password, rememberMe, lockoutOnFailure: true);

        public Task LogoutAsync() => _signInManager.SignOutAsync();
    }
}
