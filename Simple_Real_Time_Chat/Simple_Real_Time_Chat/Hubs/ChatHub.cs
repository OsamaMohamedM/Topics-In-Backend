using Microsoft.AspNetCore.SignalR;
using Simple_Real_Time_Chat.Services;

namespace Simple_Real_Time_Chat.Hubs;

public sealed class ChatHub : Hub<IChatClient>
{
    private const string UserNameQueryKey = "userName";

    private readonly IConnectionManager _connectionManager;

    public ChatHub(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userName = GetUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            Context.Abort();
            return;
        }

        _connectionManager.AddConnection(userName, Context.ConnectionId);
        await Clients.Caller.ReceiveSystemMessage($"Connected as {userName}.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = GetUserName();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            _connectionManager.RemoveConnection(userName, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(string toUserName, string message)
    {
        var fromUserName = GetUserName();
        if (string.IsNullOrWhiteSpace(fromUserName))
        {
            await Clients.Caller.ReceiveSystemMessage("A username is required before sending messages.");
            return;
        }

        var connections = _connectionManager.GetConnections(toUserName);
        if (connections.Count == 0)
        {
            await Clients.Caller.ReceiveSystemMessage($"User '{toUserName}' is offline.");
            return;
        }
        foreach (var connectionId in connections)
        {
            await Clients.Client(connectionId).ReceivePrivateMessage(fromUserName, message);
        }

        await Clients.Caller.ReceiveSystemMessage($"Private message sent to {toUserName}.");
    }

    public async Task JoinGroup(string groupName)
    {
        var normalizedGroupName = NormalizeGroupName(groupName);
        if (normalizedGroupName is null)
        {
            await Clients.Caller.ReceiveSystemMessage("Group name is required.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, normalizedGroupName);
        await Clients.Caller.ReceiveSystemMessage($"Joined group '{normalizedGroupName}'.");
    }

    public async Task LeaveGroup(string groupName)
    {
        var normalizedGroupName = NormalizeGroupName(groupName);
        if (normalizedGroupName is null)
        {
            await Clients.Caller.ReceiveSystemMessage("Group name is required.");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, normalizedGroupName);
        await Clients.Caller.ReceiveSystemMessage($"Left group '{normalizedGroupName}'.");
    }

    public async Task SendGroupMessage(string groupName, string message)
    {
        var fromUserName = GetUserName();
        var normalizedGroupName = NormalizeGroupName(groupName);

        if (string.IsNullOrWhiteSpace(fromUserName))
        {
            await Clients.Caller.ReceiveSystemMessage("A username is required before sending messages.");
            return;
        }

        if (normalizedGroupName is null)
        {
            await Clients.Caller.ReceiveSystemMessage("Group name is required.");
            return;
        }

        await Clients.Group(normalizedGroupName).ReceiveGroupMessage(normalizedGroupName, fromUserName, message);
    }

    private string? GetUserName()
    {
        var httpContext = Context.GetHttpContext();
        var userName = httpContext?.Request.Query[UserNameQueryKey].ToString();
        return string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
    }

    private static string? NormalizeGroupName(string? groupName)
    {
        return string.IsNullOrWhiteSpace(groupName) ? null : groupName.Trim();
    }
}