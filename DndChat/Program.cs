using DndChat.Data;
using DndChat.Hubs;
using DndChat.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity setup
builder.Services
    .AddIdentity<ChatUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // dev: off so login works right after register
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddControllersWithViews();

builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication comes BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

// Good to use .RequireAuthorization() on the hub endpoint. Only for Signed-in users
app.MapHub<ChatHub>("/chathub").RequireAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
