using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DndChat.Models;
using DndChat.ViewModels;

namespace DndChat.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ChatUser> _userManager;
        private readonly SignInManager<ChatUser> _signInManager;

        public AccountController(UserManager<ChatUser> userManager, SignInManager<ChatUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl});
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return View(model);
            }

            // Attempts password sign in. lockoutOnFailure helps mitigate brute force.
            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
                return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Chat")!);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is temporarily locked.");
                return View(model);
            }
            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Sign-in not allowed (email not confirmed?).");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        // GET /Account/Register
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        // POST /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ChatUser
            {
                UserName = model.UserName, // Identity enforces unique usernames
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                // Nice default for chat display if you later switch to DisplayName
                DisplayName = string.IsNullOrWhiteSpace(model.FirstName)
                    ? model.UserName
                    : $"{model.FirstName} {model.LastName}".Trim()
            };

            var create = await _userManager.CreateAsync(user, model.Password);
            if (!create.Succeeded)
            {
                foreach (var e in create.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // Auto sign in after successful registration
            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

            return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Chat")!);
        }

        // POST /Account/Logout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Chat");
        }
    }
}
