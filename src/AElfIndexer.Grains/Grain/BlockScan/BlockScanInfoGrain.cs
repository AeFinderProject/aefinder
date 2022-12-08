using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockScanInfoGrain : Grain<BlockScanInfoState>, IBlockScanInfoGrain
{
    public override Task OnActivateAsync()
    {
        this.ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        this.WriteStateAsync();
        return base.OnDeactivateAsync();
    }

    public Task<ClientInfo> GetClientInfoAsync()
    {
        return Task.FromResult(State.ClientInfo);
    }

    public Task<SubscribeInfo> GetSubscribeInfoAsync()
    {
        return Task.FromResult(State.SubscribeInfo);
    }

    public async Task SetScanNewBlockStartHeightAsync(long height)
    {
        State.ClientInfo.ScanModeInfo.ScanMode = ScanMode.NewBlock;
        State.ClientInfo.ScanModeInfo.ScanNewBlockStartHeight = height;
        await WriteStateAsync();
    }
    
    public async Task SetHandleHistoricalBlockTimeAsync(DateTime time)
    {
        State.ClientInfo.LastHandleHistoricalBlockTime = time;
        await WriteStateAsync();
    }

    public async Task InitializeAsync(string chainId, string clientId, string version, SubscribeInfo info)
    {
        var clientGrain = GrainFactory.GetGrain<IBlockScanManagerGrain>(0);
        await clientGrain.AddBlockScanAsync(chainId, this.GetPrimaryKeyString());

        State.ClientInfo = new ClientInfo
        {
            ChainId = chainId,
            ClientId = clientId,
            Version = version,
            LastHandleHistoricalBlockTime = DateTime.UtcNow,
            ScanModeInfo = new ScanModeInfo
            {
                ScanMode = ScanMode.HistoricalBlock,
                ScanNewBlockStartHeight = 0
            }
        };
        State.SubscribeInfo = info;
        await WriteStateAsync();
    }
}