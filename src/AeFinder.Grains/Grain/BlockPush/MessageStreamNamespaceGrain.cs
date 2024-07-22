using AeFinder.Grains.State.BlockPush;
using Orleans;

namespace AeFinder.Grains.Grain.BlockPush;

public class MessageStreamNamespaceGrain : Grain<MessageStreamNamespaceState>, IMessageStreamNamespaceGrain
{
    public async Task AddAppAsync(string appId)
    {
        State.AppIds.Add(appId);
        await WriteStateAsync();
    }

    public async Task<bool> ContainsAppAsync(string appId)
    {
        await ReadStateAsync();
        return State.AppIds.Contains(appId);
    }

    public Task<int> GetAppCountAsync()
    {
        return Task.FromResult(State.AppIds.Count);
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
    }
}