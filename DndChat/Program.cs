using DndChat.Data;
using DndChat.Hubs;
using DndChat.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        // Same-origin doesn't need CORS, but this future-proofs dev
        policy.WithOrigins(
             "https://localhost:7234"
          )
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

// Add ASP.NET Core Identity with the default cookie-based auth for MVC pages.
// We do NOT change the default auth scheme here, so cookies remain the default.
builder.Services
    .AddIdentity<ChatUser, IdentityRole>(options =>
    {
        // Basic password rules, no account confirmation required
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add JWT Bearer as an ADDITIONAL authentication scheme.
// This does NOT override the cookie default, it just enables Bearer when you ask for it
// Like with SignalR hubs.
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        // These values must match what you use when minting the token in AuthController.
        var config = builder.Configuration;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Expected issuer and audience for your tokens
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            // The symmetric key used to sign/verify the token
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!))
        };

        // This event lets SignalR authenticate WebSocket connections by reading the token
        // from the query string (?access_token=...). It only applies on the /chathub path.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddControllersWithViews();

// SignalR setup with detailed errors help for debugging
builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });

builder.Services.AddAuthorization();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Apply the CORS policy before auth if you’ll ever call the hub/API cross-origin
app.UseCors("Default");

// Authentication comes BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();


// Map the SignalR hub.
// Because the hub class has [Authorize(AuthenticationSchemes="Bearer,Identity.Application")],
// this RequireAuthorization() uses those schemes automatically.
app.MapHub<ChatHub>("/chathub").RequireAuthorization();

// MVC route to your Chat page which will route to login/register if not authenticated
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
