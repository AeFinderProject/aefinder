using AElfIndexer.Grains.Grain.BlockPush;
using AElfIndexer.Grains.Grain.Subscriptions;
using AElfIndexer.Grains.State.Apps;
using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.Apps;

public class AppGrain : Grain<AppState>, IAppGrain
{
    public async Task<string> AddSubscriptionAsync(SubscriptionManifest subscriptionManifest)
    {
        var newVersion = Guid.NewGuid().ToString("N");
        var subscriptionId = GetSubscriptionId(newVersion);
        await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).SetSubscriptionAsync(subscriptionManifest);

        string toRemoveSubscriptionId = null;
        if (State.CurrentVersion == null)
        {
            State.CurrentVersion = new AppVersion
            {
                Version = newVersion,
                Status = SubscriptionStatus.Initialized
            };
        }
        else
        {
            if (State.NewVersion != null)
            {
                toRemoveSubscriptionId = GetSubscriptionId(State.NewVersion.Version);
                await StopBlockPushAsync(toRemoveSubscriptionId, State.NewVersion.Version);
            }
            State.NewVersion = new AppVersion
            {
                Version = newVersion,
                Status = SubscriptionStatus.Initialized
            };
        }

        await WriteStateAsync();

        await RemoveSubscriptionAsync(toRemoveSubscriptionId);
        
        return newVersion;
    }

    public async Task UpdateSubscriptionAsync(string version, SubscriptionManifest subscriptionManifest)
    {
        if (!IsNewVersion(version) && !IsCurrentVersion(version))
        {
            return;
        }
        var subscriptionId = GetSubscriptionId(version);
        await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).SetSubscriptionAsync(subscriptionManifest);
        await WriteStateAsync();
    }

    public async Task<SubscriptionManifest> GetSubscriptionAsync(string version)
    {
        var subscriptionId = GetSubscriptionId(version);
        return await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).GetSubscriptionAsync();
    }

    public async Task<AllSubscription> GetAllSubscriptionAsync()
    {
        var result = new AllSubscription();
        if (State.CurrentVersion != null)
        {
            result.CurrentVersion = new SubscriptionDetail
            {
                Version = State.CurrentVersion.Version,
                Status = State.CurrentVersion.Status,
                SubscriptionManifest = await GrainFactory
                    .GetGrain<ISubscriptionGrain>(GetSubscriptionId(State.CurrentVersion.Version))
                    .GetSubscriptionAsync()
            };
        }

        if (State.NewVersion != null)
        {
            result.NewVersion = new SubscriptionDetail
            {
                Version = State.NewVersion.Version,
                Status = State.NewVersion.Status,
                SubscriptionManifest = await GrainFactory
                    .GetGrain<ISubscriptionGrain>(GetSubscriptionId(State.NewVersion.Version)).GetSubscriptionAsync()
            };
        }

        return result;
    }

    public async Task<byte[]> GetCodeAsync(string version)
    {
        var codeId = GetAppCodeId(version);
        return await GrainFactory.GetGrain<IAppCodeGrain>(codeId).GetCodeAsync();
    }

    public async Task<bool> IsRunningAsync(string version, string chainId, string scanToken)
    {
        var appVersion = GetAppVersion(version);
        if (appVersion == null)
        {
            return false;
        }
        if(appVersion.Status != SubscriptionStatus.Started)
        {
            return false;
        }

        if (!await GrainFactory
                .GetGrain<IBlockPusherInfoGrain>(
                    GrainIdHelper.GenerateBlockPusherGrainId(this.GetPrimaryKeyString(), version, chainId))
                .IsRunningAsync(scanToken))
        {
            return false;
        }

        return true;
    }

    public async Task UpgradeVersionAsync()
    {
        if (State.NewVersion == null)
        {
            return;
        }

        string toRemoveSubscriptionId = null;
        if (State.CurrentVersion != null)
        {
            toRemoveSubscriptionId = GetSubscriptionId(State.CurrentVersion.Version);
            await StopBlockPushAsync(toRemoveSubscriptionId, State.CurrentVersion.Version);
        }

        State.CurrentVersion = new AppVersion
        {
            Version = State.NewVersion.Version,
            Status = State.NewVersion.Status
        };
        State.NewVersion = null;
        await WriteStateAsync();
        
        await RemoveSubscriptionAsync(toRemoveSubscriptionId);
    }

    public Task<SubscriptionStatus> GetSubscriptionStatusAsync(string version)
    {
        return Task.FromResult(GetAppVersion(version).Status);
    }

    public async Task StartAsync(string version)
    {
        var appVersion = GetAppVersion(version);
        appVersion.Status = SubscriptionStatus.Started;
        await WriteStateAsync();
    }
    
    public async Task PauseAsync(string version)
    {
        var appVersion = GetAppVersion(version);
        appVersion.Status = SubscriptionStatus.Paused;
        await WriteStateAsync();
    }

    public async Task StopAsync(string version)
    {
        if (IsCurrentVersion(version))
        {
            State.CurrentVersion = null;
        }
        else if (IsNewVersion(version))
        {
            State.NewVersion = null;
        }
        else
        {
            return;
        }
        
        var toRemoveSubscriptionId = GetSubscriptionId(version);;
        await StopBlockPushAsync(toRemoveSubscriptionId, version);

        await WriteStateAsync();
        
        await RemoveSubscriptionAsync(toRemoveSubscriptionId);
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
    
    private string GetSubscriptionId(string version)
    {
        return GrainIdHelper.GenerateSubscriptionGrainId(this.GetPrimaryKeyString(), version);
    }
    
    private string GetAppCodeId(string version)
    {
        return GrainIdHelper.GenerateGetAppCodeGrainId(this.GetPrimaryKeyString(), version);
    }
    
    private async Task StopBlockPushAsync(string subscriptionId, string version)
    {
        var subscriptionGrain = GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId);
        var subscription = await subscriptionGrain.GetSubscriptionAsync();
        foreach (var item in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(this.GetPrimaryKeyString(), version, item.ChainId);
            await GrainFactory.GetGrain<IBlockPusherInfoGrain>(id).StopAsync();
        }
    }
    
    private async Task RemoveSubscriptionAsync(string subscriptionId)
    {
        if (!string.IsNullOrWhiteSpace(subscriptionId))
        {
            await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).RemoveAsync();
        }
    }
    
    private bool IsCurrentVersion(string version)
    {
        return State.CurrentVersion != null && State.CurrentVersion.Version == version;
    }
    
    private bool IsNewVersion(string version)
    {
        return State.NewVersion != null && State.NewVersion.Version == version;
    }
    
    private AppVersion GetAppVersion(string version)
    {
        if (IsCurrentVersion(version))
        {
            return State.CurrentVersion;
        }

        if (IsNewVersion(version))
        {
            return State.NewVersion;
        }

        return null;
    }
}