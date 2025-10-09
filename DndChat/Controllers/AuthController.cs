using DndChat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace DndChat.Controllers
{
    [Route("api/auth")]
    [ApiController]

    // primary constructor syntax
    public class AuthController(UserManager<ChatUser> userManager, IConfiguration config) : ControllerBase
    {
        // Identity user manager to load users/roles from the DB
        private readonly UserManager<ChatUser> _users = userManager;
        // App config (Issuer, Audience, SecretKey, ExpirationInMinutes)
        private readonly IConfiguration _config = config;

        // Cookie-protected endpoint that "mints" a JWT for the current user
        // Only allow callers that are ALREADY authenticated via the Identity cookie
        [Authorize]
        [HttpPost("token")]
        public async Task<IActionResult> GetToken()
        {
            // Read the current user id from the cookie principal
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            // Load the full user from the database
            var user = await _users.FindByIdAsync(userId);
            if (user is null) return Unauthorized();

            // Build the claims that will go into the JWT
            var claims = new List<Claim>
        {
                // .Sub is subject, to uniquely identify the entity (usually the user) that the token is about
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? "")
        };
            // Add any global roles (e.g., "Member", "SiteAdmin")
            // Will be useful later on for private chat rooms
            var roles = await _users.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // Create signing credentials from the symmetric secret in secrets file
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var exp = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpirationInMinutes"]!));

            // Assemble the JWT (issuer, audience, claims, expiry, signature)
            var jwt = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: exp,
                signingCredentials: creds);
            // Serialize token and return JSON { accessToken, expiresAtUtc }
            return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(jwt), expiresAtUtc = exp });
        }

    }
}
