using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Grains.State.BlockPush;
using Microsoft.Extensions.Options;
using Orleans;

namespace AeFinder.Grains.Grain.BlockPush;

public class BlockPusherInfoGrain : AeFinderGrain<BlockPusherInfoState>, IBlockPusherInfoGrain
{
    private readonly BlockPushOptions _blockPushOptions;

    public BlockPusherInfoGrain(IOptionsSnapshot<BlockPushOptions> blockPushOptions)
    {
        _blockPushOptions = blockPushOptions.Value;
    }

    public async Task<BlockPushInfo> GetPushInfoAsync()
    {
        await ReadStateAsync();
        return State.BlockPushInfo;
    }

    public async Task<Subscription> GetSubscriptionAsync()
    {
        await ReadStateAsync();
        return State.Subscription;
    }

    public async Task SetNewBlockStartHeightAsync(long height)
    {
        await ReadStateAsync();
        State.BlockPushMode = BlockPushMode.NewBlock;
        State.NewBlockStartHeight = height;
        await WriteStateAsync();
    }
    
    public async Task SetHistoricalBlockPushModeAsync()
    {
        await ReadStateAsync();
        State.BlockPushMode = BlockPushMode.HistoricalBlock;
        State.NewBlockStartHeight = 0;
        await WriteStateAsync();
    }
    
    public async Task SetHandleHistoricalBlockTimeAsync(DateTime time)
    {
        await ReadStateAsync();
        
        State.LastHandleHistoricalBlockTime = time;
        await WriteStateAsync();
    }

    public async Task InitializeAsync(string appId, string version, Subscription item, string pushToken)
    {
        await ReadStateAsync();
        
        var pusherManagerGrain = GrainFactory.GetGrain<IBlockPusherManagerGrain>(0);
        await pusherManagerGrain.AddBlockPusherAsync(item.ChainId, this.GetPrimaryKeyString());

        State.BlockPushInfo = new BlockPushInfo
        {
            AppId = appId,
            Version = version,
            PushToken = pushToken
        };
        State.Subscription = item;
        State.BlockPushMode = BlockPushMode.HistoricalBlock;
        State.NewBlockStartHeight = 0;
        State.LastHandleHistoricalBlockTime = DateTime.UtcNow;
        await WriteStateAsync();
    }

    public async Task UpdateSubscriptionInfoAsync(Subscription item)
    {
        await ReadStateAsync();
        
        State.Subscription = item;
        await WriteStateAsync();
    }

    public async Task StopAsync()
    {
        var blockPusherManagerGrain = GrainFactory.GetGrain<IBlockPusherManagerGrain>(0);
        await blockPusherManagerGrain.RemoveBlockPusherAsync(State.Subscription.ChainId, this.GetPrimaryKeyString());
    }
    
    public async Task<Guid> GetMessageStreamIdAsync()
    {
        await ReadStateAsync();
        
        if (State.MessageStreamId == Guid.Empty)
        {
            State.MessageStreamId = Guid.NewGuid();
            await WriteStateAsync();
        }

        return State.MessageStreamId;
    }

    public async Task<bool> IsPushBlockAsync(long blockHeight, bool isConfirmedBlock)
    {
        await ReadStateAsync();
        return State.BlockPushMode == BlockPushMode.NewBlock &&
               State.NewBlockStartHeight <= blockHeight &&
               (isConfirmedBlock || !State.Subscription.OnlyConfirmed);
    }

    public async Task<BlockPushMode> GetPushModeAsync()
    {
        await ReadStateAsync();
        return State.BlockPushMode;
    }
    
    public async Task<bool> IsNeedRecoverAsync()
    {
        await ReadStateAsync();
        return State.BlockPushMode == BlockPushMode.HistoricalBlock && State.LastHandleHistoricalBlockTime <
            DateTime.UtcNow.AddMinutes(-_blockPushOptions.HistoricalPushRecoveryThreshold);
    }

    public async Task<bool> IsRunningAsync(string pushToken)
    {
        await ReadStateAsync();
        return State.BlockPushInfo.PushToken == pushToken;
    }
    
    public async Task<string> GetPushTokenAsync()
    {
        await ReadStateAsync();
        return State.BlockPushInfo.PushToken;
    }
    
    public async Task ClearGrainStateAsync()
    {
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}