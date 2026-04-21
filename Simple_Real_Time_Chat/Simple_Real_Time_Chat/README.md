# Simple Real-Time Chat (SignalR)

A simple educational ASP.NET Core project that demonstrates real-time chat communication using SignalR.

## What this project demonstrates

- Real-time 1-to-1 messaging
- Real-time group messaging
- Joining and leaving chat groups dynamically
- Strongly typed Hub-to-client communication (no magic strings for client callbacks)
- Clean separation of concerns with in-memory connection tracking service

## Tech stack and tools

- `.NET 10` (`ASP.NET Core`)
- `SignalR` for real-time communication
- `MessagePack` protocol for compact binary payloads
- Vanilla JavaScript frontend (`wwwroot/index.html`)
- In-memory thread-safe collections (`ConcurrentDictionary`)

## Project structure

- `Program.cs`
  - Configures services and middleware
  - Registers SignalR and maps `/chathub`
  - Enables CORS and static file hosting

- `Hubs/`
  - `ChatHub.cs`: main real-time chat hub (private + group messaging)
  - `IChatClient.cs`: strongly typed client contract for messages

- `Services/`
  - `IConnectionManager.cs`: abstraction for connection mapping
  - `ConnectionManager.cs`: thread-safe in-memory implementation

- `wwwroot/`
  - `index.html`: simple UI to connect and test private/group messaging

- `Controllers/`
  - API endpoints used by the app

- `appsettings.json`
  - environment configuration values

## Core real-time concepts used

- **Hub lifecycle**
  - `OnConnectedAsync` and `OnDisconnectedAsync` to track active connections

- **User-to-connection mapping**
  - One user can have one or many active `ConnectionId`s
  - Mapping is handled outside the Hub to keep responsibilities focused

- **Private messaging**
  - Routes messages directly using `Clients.Client(connectionId)`

- **Group messaging**
  - Uses `Groups.AddToGroupAsync` / `Groups.RemoveFromGroupAsync`
  - Broadcasts with `Clients.Group(groupName)`

- **Strongly typed clients**
  - Hub inherits from `Hub<IChatClient>`
  - Client callbacks are compile-time safe

## How to run

From the `Simple_Real_Time_Chat` directory:

```bash
dotnet restore
dotnet run
```

Then open the URL shown in terminal (for example: `https://localhost:7xxx`).

## Quick manual test

1. Open the app in two browser windows.
2. Connect from both windows using different usernames.
3. Send a private message from one user to the other.
4. Join the same group in both windows.
5. Send a group message and verify both clients receive it.

## Notes

- This is an educational sample focused on real-time communication patterns.
- Connection state is in-memory and resets when the app restarts.