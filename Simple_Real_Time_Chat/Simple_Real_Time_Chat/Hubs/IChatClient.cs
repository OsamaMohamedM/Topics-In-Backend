namespace Simple_Real_Time_Chat.Hubs;

public interface IChatClient
{
    Task ReceivePrivateMessage(string fromUserName, string message);

    Task ReceiveGroupMessage(string groupName, string fromUserName, string message);

    Task ReceiveSystemMessage(string message);
}