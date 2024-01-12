using AElfIndexer.Grains.State.BlockScanExecution;
using AElfIndexer.Grains.State.Subscriptions;
using Microsoft.Extensions.Options;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public class BlockScanGrain : Grain<BlockScanState>, IBlockScanGrain
{
    private readonly BlockScanOptions _blockScanOptions;

    public BlockScanGrain(IOptionsSnapshot<BlockScanOptions> blockScanOptions)
    {
        _blockScanOptions = blockScanOptions.Value;
    }

    public async Task<ScanInfo> GetScanInfoAsync()
    {
        await ReadStateAsync();
        return State.ScanInfo;
    }

    public async Task<SubscriptionItem> GetSubscriptionAsync()
    {
        await ReadStateAsync();
        return State.SubscriptionItem;
    }

    public async Task SetScanNewBlockStartHeightAsync(long height)
    {
        State.ScanMode = ScanMode.NewBlock;
        State.ScanNewBlockStartHeight = height;
        await WriteStateAsync();
    }
    
    public async Task SetHistoricalBlockScanModeAsync()
    {
        State.ScanMode = ScanMode.HistoricalBlock;
        State.ScanNewBlockStartHeight = 0;
        await WriteStateAsync();
    }
    
    public async Task SetHandleHistoricalBlockTimeAsync(DateTime time)
    {
        State.LastHandleHistoricalBlockTime = time;
        await WriteStateAsync();
    }

    public async Task InitializeAsync(string clientId, string version, SubscriptionItem item, string scanToken)
    {
        var blockScanManager = GrainFactory.GetGrain<IBlockScanManagerGrain>(0);
        await blockScanManager.AddBlockScanAsync(item.ChainId, this.GetPrimaryKeyString());

        State.ScanInfo = new ScanInfo
        {
            ScanAppId = clientId,
            Version = version,
            ScanToken = scanToken
        };
        State.SubscriptionItem = item;
        State.ScanMode = ScanMode.HistoricalBlock;
        State.ScanNewBlockStartHeight = 0;
        State.LastHandleHistoricalBlockTime = DateTime.UtcNow;
        await WriteStateAsync();
    }

    public async Task UpdateSubscriptionInfoAsync(SubscriptionItem item)
    {
        State.SubscriptionItem = item;
        await WriteStateAsync();
    }

    public async Task StopAsync()
    {
        var blockScanManager = GrainFactory.GetGrain<IBlockScanManagerGrain>(0);
        await blockScanManager.RemoveBlockScanAsync(State.SubscriptionItem.ChainId, this.GetPrimaryKeyString());
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

    public async Task<bool> IsScanBlockAsync(long blockHeight, bool isConfirmedBlock)
    {
        await ReadStateAsync();
        return State.ScanMode == ScanMode.NewBlock &&
               State.ScanNewBlockStartHeight <= blockHeight &&
               (isConfirmedBlock || !State.SubscriptionItem.OnlyConfirmed);
    }

    public async Task<ScanMode> GetScanModeAsync()
    {
        await ReadStateAsync();
        return State.ScanMode;
    }
    
    public async Task<bool> IsNeedRecoverAsync()
    {
        await ReadStateAsync();
        return State.ScanMode == ScanMode.HistoricalBlock && State.LastHandleHistoricalBlockTime >=
            DateTime.UtcNow.AddMinutes(-_blockScanOptions.HistoricalPushRecoveryThreshold);
    }

    public async Task<bool> IsRunningAsync(string token)
    {
        await ReadStateAsync();
        return State.ScanInfo.ScanToken == token;
    }
    
    public async Task<string> GetScanTokenAsync()
    {
        await ReadStateAsync();
        return State.ScanInfo.ScanToken;
    }
}