using System.Collections.Concurrent;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Hubs;

public interface IConnectionProvider
{
    void Add(string clientId, string connectionId, string version, List<string> chainIds);
    void Remove(string connectionId);
    ConnectionInfo GetConnectionByClientId(string clientId);
    ConnectionInfo GetConnectionByConnectionId(string connectionId);
}

public class ConnectionProvider : IConnectionProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<string, string> _connectionIds = new();

    public void Add(string clientId, string connectionId, string version, List<string> chainIds)
    {
        _connections[clientId] = new ConnectionInfo
        {
            Version = version,
            ConnectionId = connectionId,
            ClientId = clientId,
            ChainIds = chainIds
        };
        _connectionIds[connectionId] = clientId;
    }
    
    public void Remove(string connectionId)
    {
        if (_connectionIds.TryGetValue(connectionId, out var clientId))
        {
            _connections.TryRemove(clientId, out _);
            _connectionIds.TryRemove(connectionId, out _);
        }
    }

    public ConnectionInfo GetConnectionByClientId(string clientId)
    {
        if (_connections.TryGetValue(clientId, out var connection))
        {
            return connection;
        }

        return null;
    }
    
    public ConnectionInfo GetConnectionByConnectionId(string connectionId)
    {
        if (_connectionIds.TryGetValue(connectionId, out var clientId))
        {
            return GetConnectionByClientId(clientId);
        }
        
        return null;
    }
}

public class ConnectionInfo
{
    public string ConnectionId { get; set; }
    public string ClientId { get; set; }
    public string Version { get; set; }
    public List<string> ChainIds { get; set; } = new();
}