# Simple Real-Time Chat (SignalR)

Educational ASP.NET Core chat sample with:

- 1-to-1 private messaging
- Group join/leave and group messaging
- Strongly typed SignalR hub (`Hub<IChatClient>`)
- Thread-safe in-memory connection tracking (`IConnectionManager`)

## Prerequisites

- .NET SDK 10

## Run

From the `Simple_Real_Time_Chat` folder:

```bash
dotnet restore
dotnet run
```

After startup, open the app URL shown in terminal (example: `https://localhost:7xxx`).

The UI is served from `wwwroot/index.html` automatically.

## Quick test flow

1. Open the app in two browser windows (or one normal + one incognito).
2. In window 1, connect as `Alice`.
3. In window 2, connect as `Bob`.
4. Send private message from `Alice` to `Bob`.
5. In both windows, join group `study-group`.
6. Send a group message to `study-group`.

## Notes

- Connection/user mapping is in-memory only (resets on app restart).
- No authentication/JWT/database is used by design for learning.
- If you serve frontend from another origin (for example `http://127.0.0.1:5500`), keep/update the CORS policy in `Program.cs`.