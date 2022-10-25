using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Hubs;

public interface IConnectionProvider
{
    void Add(string clientId, string connectionId);
    void Remove(string clientId);
    string GetConnectionId(string clientId);
}

public class ConnectionProvider : IConnectionProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, string> _connectionIds = new();

    public void Add(string clientId, string connectionId)
    {
        _connectionIds[clientId] = connectionId;
    }
    
    public void Remove(string clientId)
    {
        _connectionIds.TryRemove(clientId, out _);
    }

    public string GetConnectionId(string clientId)
    {
        if (_connectionIds.TryGetValue(clientId, out var connectionId))
        {
            return connectionId;
        }

        return null;
    }
}