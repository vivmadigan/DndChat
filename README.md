# DndChat

A lightweight real-time chat for Dungeons & Dragons groups. Players **register & sign in** with ASP.NET Core Identity. Once logged in, they join a **global chat** (SignalR). The chat shows lines when users send messages and when someone **logs in** or **logs out**.

---

## How it works

1. User **registers/signs in** (Identity).
2. Opening the chat page starts a **SignalR** connection to `/chathub` (JWT if available, otherwise cookie).
3. The server adds the connection to a **global** group and broadcasts a small **system notice** on connect/disconnect.
4. Messages are sent via the hub and broadcast to all clients in the global group.  
   _Security:_ chat lines use `textContent`; only status banners use `innerHTML` and are sanitized with **DOMPurify**.

---

## Features (current)

- Secure register/sign-in with **ASP.NET Core Identity**
- **Global chat** with SignalR (auto WebSocket/SSE/long-poll fallback)
- System messages for **login** / **logout**
- Only **authenticated** users can send; messages display the user’s **username**
- Client-side XSS hardening with **DOMPurify**
- Basic server/hub **console logging**

---

## Roadmap

- **Private rooms** (GUID invite + membership checks)
- **Message history** persisted to database (load last N on join)
- Structured logging (e.g., NLog) and a few unit tests
- Optional end-to-end **message encryption** (app-level)

---

## Getting started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 (or `dotnet` CLI)

### Configure (dev)
- Ensure your SQL connection string `DefaultConnection` is set in `appsettings.Development.json`.
- If you’re using JWT for the SignalR client, define:
  ```json
  "Jwt": {
    "Issuer": "https://localhost:7240",
    "Audience": "https://localhost:7240",
    "SecretKey": "dev-only-very-long-random-string"
  }


### Run

Using Visual Studio:
1. Restore client libraries with LibMan if prompted.
2. Press F5 to run.
3. Open a second browser tab to the same URL to see real time messages.

Using CLI:
```bash
dotnet restore
dotnet ef database update 
dotnet run
