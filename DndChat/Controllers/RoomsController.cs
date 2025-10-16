using DndChat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DndChat.Models;
using DndChat.Data;
using Microsoft.AspNetCore.Identity;
using DndChat.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DndChat.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        // DI: room service (create/join/lookup)
        private readonly IChatRoomService _chatRooms;
        // DI: identity helper to read current user's ID safely
        private readonly UserManager<ChatUser> _users;

        //To broadcast that a room was deleted
        private readonly IHubContext<ChatHub> _hub;

        public RoomsController(IChatRoomService chatRooms,
                               UserManager<ChatUser> users,
                               IHubContext<ChatHub> hub)
        {
            _chatRooms = chatRooms;
            _users = users;
            _hub = hub;
        }
        // A simple screen with two cards: Join or Create.
        public async Task<IActionResult> Index()
        {
            var userId = _users.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var myRooms = await _chatRooms.RoomsForUserListAsync(userId);
            return View(myRooms);
        }

        // Creates a room with the given name and redirects to /Rooms/Room/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string? roomName)
        {
            // Get the current signed-in user's ID (Identity's User.Id).
            // This wraps the NameIdentifier claim and returns null if missing.
            var userId = _users.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Ask the service to create a private room owned by this user.
            // It returns a tuple (roomId, joinCode). We only need roomId here,
            // so discard the second value with "_".
            var (roomId, _) = await _chatRooms.CreatePrivateAsync(userId, roomName);

            // Redirect to the dedicated room page action "Room".
            // nameof(Room) is refactor-safe (compiler-checked) vs a raw "Room" string.
            return RedirectToAction(nameof(Room), new { id = roomId });

        }
        // Looks up a room by the code the user typed and redirects to it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(string joinCode)
        {
            var userId = _users.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Resolve code -> room id
            var roomId = await _chatRooms.ResolveByJoinCodeAsync(joinCode);
            if (roomId is null)
            {
                TempData["JoinError"] = "Invalid code. Double check and try again.";
                return RedirectToAction(nameof(Index));
            }

            // Ensure this user is a member BEFORE showing the room page
            await _chatRooms.AddMemberAsync(userId, roomId);

            return RedirectToAction(nameof(Room), new { id = roomId });
        }
        // Optional: leave a room (HTTP version; your hub also has LeaveRoom for SignalR group only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(string id)
        {
            var userId = _users.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _chatRooms.RemoveMemberAsync(userId, id);
            TempData["JoinError"] = "You left the room."; // small confirmation
            return RedirectToAction(nameof(Index));
        }
        // GET /Rooms/Room/{id}
        // Renders the room page; the page's JS will connect to the hub and call JoinByCode(id).
        [HttpGet("/Rooms/Room/{id}")]
        public async Task<IActionResult> Room(string id)
        {
            var userId = _users.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var header = await _chatRooms.GetRoomHeaderForUserAsync(userId, id);
            if (header is null)
            {
                TempData["JoinError"] = "You don’t have access to that room. Enter the code to join.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["RoomId"] = header.Id;
            ViewData["RoomName"] = header.Name;
            ViewData["JoinCode"] = header.JoinCode;
            ViewData["IsOwner"] = header.IsOwner;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = _users.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ok = await _chatRooms.DeleteRoomAsync(userId, id);
            if (ok)
            {
                // Tell anyone currently in that SignalR group that the room is gone
                await _hub.Clients.Group(id).SendAsync("RoomDeleted", id, "This room was deleted by the owner.");
                TempData["JoinError"] = "Room deleted.";
            }
            else
            {
                TempData["JoinError"] = "You don’t have permission to delete that room (or it no longer exists).";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
