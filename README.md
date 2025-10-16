# DndChat

At the moment, a lightweight real-time chat for Dungeons & Dragons groups.  Players **register & sign in** with ASP.NET Core Identity. After login they can chat in the **Global chat** or create/join **private rooms** via a shareable join code. SignalR powers live updates; EF Core stores rooms, memberships, and message history. In the future expanding to a platform for players to not only chat about their campaigns and find other players, but to also get inspiration for their next campaign by creating fun character with the help of AI to bring creativity to life.

---

## How it works

1. **Auth** – Users register/sign in with ASP.NET Core Identity (cookie auth).
2. **SignalR** – Pages connect to `/chathub` (JWT if available, otherwise cookie).
3. **Global chat** – The global page calls `JoinGlobal`; messages broadcast to the “global” SignalR group.
4. **Private rooms** – Go to `/Rooms` to **Create** a room (you get a code) or **Join** one by code.  
   The room page calls `JoinByCode(roomId)`, loads the last N messages, and streams new ones live.
5. **Persistence** – `ChatRoom`, `ChatMembership`, and `ChatMessage` entities store all state.
6. **Safety** – Chat lines render with `textContent`. Status banners use sanitized `innerHTML` (DOMPurify).

---

## Features

- ✅ Secure register/sign-in with **ASP.NET Core Identity**
- ✅ **Global chat** with connect/disconnect system notices
- ✅ **Private rooms** with shareable **join codes** (MVP: code == roomId)
- ✅ **Message history** in SQL; last N messages load on join
- ✅ “**Your rooms**” list (owner/member), quick **copy code**, and **leave** room
- ✅ Auto-reconnect and rejoin logic (global/room)
- ✅ Client-side XSS hardening with **DOMPurify**
- ✅ Basic server/hub **console logging**

---

## Roadmap

- **Dashboard landing page** after login (recent rooms, quick actions)
- **OpenAI integration**: suggest character ideas based on selected categories (class, theme, party role)
- **Owner-only delete room** (cascade members/messages) with confirm dialog & UI polish
- Structured logging (Serilog/NLog) and unit tests
- Optional end-to-end **message encryption** (app-level)

---

## Architecture

- **Controllers**
  - `ChatController` – Global chat page
  - `RoomsController` – Join/Create screen and dedicated room page
  - `AuthController` – Cookie-protected endpoint that mints short-lived JWTs for SignalR
- **Hub**
  - `ChatHub` – `JoinGlobal`, `SendMessage`, `CreatePrivateRoom`, `JoinByCode`, `SendRoomMessage`, `LeaveRoom`
- **Service layer**
  - `IChatRoomService` / `ChatRoomService` – Create/resolve rooms, manage membership, save/read history
- **EF Core models**
  - `ChatRoom`, `ChatMembership`, `ChatMessage`
  - Constraints: unique `(ChatRoomId, UserId)` on memberships; unique `JoinCode`
  - A persistent **Global** room is seeded at startup
- **Client**
  - `wwwroot/js/chat.js` – Global chat page
  - `wwwroot/js/chat-room.js` – Private room page (join code, history, live stream)

---

## Getting started

### Prerequisites

- .NET 8 SDK  
- Visual Studio 2022 (or `dotnet` CLI)  
- SQL Server (LocalDB or full)

### Configure (dev)

- Set your connection string `DefaultConnection` in `appsettings.Development.json`.
- Optional JWT settings used by the SignalR client:

```json
"Jwt": {
  "Issuer": "https://localhost:7240",
  "Audience": "https://localhost:7240",
  "SecretKey": "dev-only-very-long-random-string",
  "ExpirationInMinutes": 30
}
```

### Database 

dotnet ef database update

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
```

