using Microsoft.AspNetCore.SignalR;

namespace DndChat.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            // Invokes the client-side handler 'ReceiveMessage' on all connected clients (server → client).
            // The first argument is the client method name; the remaining arguments are passed to that JS callback.
            // 'ReceiveMessage' is registered in wwwroot/js/chat.js (a javascript file) via: conn.on('ReceiveMessage', (user, message) => { ... });
            //
            // End-to-end flow:
            //   client → server:  conn.invoke('SendMessage', user, text) calls this C# hub method
            //   server → clients: Clients.All.SendAsync("ReceiveMessage", user, text) broadcasts to browsers
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
