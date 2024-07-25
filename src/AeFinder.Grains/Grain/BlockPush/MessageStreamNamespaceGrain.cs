using AeFinder.Grains.State.BlockPush;
using Orleans;

namespace AeFinder.Grains.Grain.BlockPush;

public class MessageStreamNamespaceGrain : AeFinderGrain<MessageStreamNamespaceState>, IMessageStreamNamespaceGrain
{
    public async Task AddAppAsync(string appId)
    {
        await ReadStateAsync();
        State.AppIds.Add(appId);
        await WriteStateAsync();
    }

    public async Task<bool> ContainsAppAsync(string appId)
    {
        await ReadStateAsync();
        return State.AppIds.Contains(appId);
    }

    public async Task<int> GetAppCountAsync()
    {
        await ReadStateAsync();
        return State.AppIds.Count;
    }
}