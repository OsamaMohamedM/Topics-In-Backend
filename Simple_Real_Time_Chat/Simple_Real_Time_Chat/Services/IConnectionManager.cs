namespace Simple_Real_Time_Chat.Services;

public interface IConnectionManager
{
    void AddConnection(string userName, string connectionId);

    void RemoveConnection(string userName, string connectionId);

    IReadOnlyCollection<string> GetConnections(string userName);
}