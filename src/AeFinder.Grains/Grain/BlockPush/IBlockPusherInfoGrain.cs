using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Grains.State.BlockPush;
using Orleans;

namespace AeFinder.Grains.Grain.BlockPush;

public interface IBlockPusherInfoGrain : IGrainWithStringKey
{
    Task<BlockPushInfo> GetPushInfoAsync();
    Task<Subscription> GetSubscriptionAsync();
    Task SetNewBlockStartHeightAsync(long height);
    Task SetHandleHistoricalBlockTimeAsync(DateTime time);
    Task SetHistoricalBlockPushModeAsync();
    Task InitializeAsync(string appId, string version, Subscription item, string pushToken);
    Task UpdateSubscriptionInfoAsync(Subscription info);
    Task StopAsync();
    Task<Guid> GetMessageStreamIdAsync();
    Task<bool> IsPushBlockAsync(long blockHeight, bool isConfirmed);
    Task<BlockPushMode> GetPushModeAsync();
    Task<bool> IsNeedRecoverAsync();
    Task<bool> IsRunningAsync(string pushToken);
    Task<string> GetPushTokenAsync();
    Task ClearGrainStateAsync();
}