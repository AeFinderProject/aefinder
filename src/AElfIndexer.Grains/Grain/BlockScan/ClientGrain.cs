using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class ClientGrain : Grain<ClientState>, IClientGrain
{
    public async Task<string> SubscribeAsync(List<SubscribeInfo> subscribeInfos)
    {
        State.SubscribeInfos = subscribeInfos;
        var newVersion = Guid.NewGuid().ToString("N");
        State.NewVersion = newVersion;
        if (string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            State.CurrentVersion = newVersion;
        }

        return newVersion;
    }

    public async Task<Guid> GetMessageStreamIdAsync()
    {
        if (State.MessageStreamId == Guid.Empty)
        {
            State.MessageStreamId = Guid.NewGuid();
            await WriteStateAsync();
        }

        return State.MessageStreamId;
    }

    // public async Task SetBlockScanIdsAsync(string version, HashSet<string> ids)
    // {
    //     if (version != State.NewVersion && version != State.CurrentVersion)
    //     {
    //         return;
    //     }
    //
    //     State.BlockScanIds[version] = ids;
    //     await WriteStateAsync();
    // }

    public async Task<bool> IsVersionAvailableAsync(string version)
    {
        return version != State.NewVersion || version != State.CurrentVersion;
    }

    public async Task<string> GetCurrentVersionAsync()
    {
        return State.CurrentVersion;
    }

    public async Task<string> GetNewVersionAsync()
    {
        return State.NewVersion;
    }

    public async Task UpgradeVersionAsync()
    {
        State.CurrentVersion = State.NewVersion;
        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
}