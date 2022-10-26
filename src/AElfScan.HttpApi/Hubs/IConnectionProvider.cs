using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Hubs;

public interface IConnectionProvider
{
    void Add(string clientId, string connectionId, string version);
    void Remove(string clientId);
    ConnectionInfo GetConnection(string clientId);
}

public class ConnectionProvider : IConnectionProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connectionIds = new();

    public void Add(string clientId, string connectionId, string version)
    {
        _connectionIds[clientId] = new ConnectionInfo
        {
            Version = version,
            ConnectionId = connectionId
        };
    }
    
    public void Remove(string clientId)
    {
        _connectionIds.TryRemove(clientId, out _);
    }

    public ConnectionInfo GetConnection(string clientId)
    {
        if (_connectionIds.TryGetValue(clientId, out var connection))
        {
            return connection;
        }

        return null;
    }
}

public class ConnectionInfo
{
    public string ConnectionId { get; set; }
    public string Version { get; set; }
}