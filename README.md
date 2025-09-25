# DndChat

DndChat is a simple web app for Dungeons & Dragons players. The goal is to let players log in, meet in a main chat room, and create private rooms for their own campaigns.

## Status

MVP in progress:
- Join with a username to access a global chat
- Real time messaging with SignalR
- ASP.NET Core MVC project structure

Planned next:
- Proper login with ASP.NET Core Identity
- Private chat rooms
- Message history and basic moderation tools

## Tech stack

- ASP.NET Core MVC
- SignalR for real time communication (WebSockets with SSE and long polling fallback)
- ASP.NET Core Identity (planned)

## How it works (MVP)

1. User opens `/Chat/Index`, enters a username with 3 or more characters, and submits the form.
2. The server re-renders the same view with `Joined = true`.
3. A small script (`wwwroot/js/chat.js`) starts a SignalR connection to `/chathub`.
4. Messages are sent with `SendMessage(user, message)` and broadcast to all clients with `ReceiveMessage`.

Security note: messages are rendered with `textContent` on the client and Razor encoding on the server to avoid XSS.

## Getting started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or `dotnet` CLI

### Run

Using Visual Studio:
1. Restore client libraries with LibMan if prompted.
2. Press F5 to run.
3. Open a second browser tab to the same URL to see real time messages.

Using CLI:
```bash
dotnet restore
dotnet run
