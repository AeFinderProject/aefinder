using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockScanExecution;
using AElfIndexer.Grains.Grain.Subscriptions;
using AElfIndexer.Grains.State.ScanApps;
using Orleans;

namespace AElfIndexer.Grains.Grain.ScanApps;

public class ScanAppGrain : Grain<ScanAppState>, IScanAppGrain
{
    public async Task<string> AddSubscriptionAsync(Subscription subscription)
    {
        var newVersion = Guid.NewGuid().ToString("N");
        var subscriptionId = GetSubscriptionId(newVersion);
        await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).SetSubscriptionAsync(subscription);
        State.VersionStatus[newVersion] = VersionStatus.Initialized;

        string toRemoveSubscriptionId = null;
        if (string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            State.CurrentVersion = newVersion;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(State.NewVersion))
            {
                toRemoveSubscriptionId = GetSubscriptionId(State.NewVersion);
                await StopBlockScanAsync(toRemoveSubscriptionId, State.NewVersion);
                State.VersionStatus.Remove(State.NewVersion);
            }
            State.NewVersion = newVersion;
        }

        await WriteStateAsync();

        await RemoveSubscriptionAsync(toRemoveSubscriptionId);
        
        return newVersion;
    }

    public async Task UpdateSubscriptionAsync(string version, Subscription subscription)
    {
        if (version != State.NewVersion && version != State.CurrentVersion)
        {
            return;
        }
        var subscriptionId = GetSubscriptionId(version);
        await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).SetSubscriptionAsync(subscription);
        await WriteStateAsync();
    }

    public async Task<Subscription> GetSubscriptionAsync(string version)
    {
        var subscriptionId = GetSubscriptionId(version);
        return await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).GetSubscriptionAsync();
    }

    public async Task<AllSubscriptionDto> GetAllSubscriptionsAsync()
    {
        var result = new AllSubscriptionDto();
        if (!string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            result.CurrentVersion = new AllSubscriptionDetailDto
            {
                Version = State.CurrentVersion,
                Subscription = await GrainFactory.GetGrain<ISubscriptionGrain>(GetSubscriptionId(State.CurrentVersion)).GetSubscriptionAsync()
            };
        }
        
        if (!string.IsNullOrWhiteSpace(State.NewVersion))
        {
            result.NewVersion = new AllSubscriptionDetailDto
            {
                Version = State.NewVersion,
                Subscription = await GrainFactory.GetGrain<ISubscriptionGrain>(GetSubscriptionId(State.NewVersion)).GetSubscriptionAsync()
            };
        }

        return result;
    }

    public async Task<bool> IsRunningAsync(string version, string chainId, string scanToken)
    {
        return !string.IsNullOrWhiteSpace(version) &&
               (version == State.NewVersion || version == State.CurrentVersion) &&
               State.VersionStatus[version] == VersionStatus.Started &&
               await GrainFactory
                   .GetGrain<IBlockScanGrain>(GrainIdHelper.GenerateGrainId(chainId, this.GetPrimaryKeyString(),
                       version)).IsRunningAsync(scanToken);
    }

    public async Task UpgradeVersionAsync()
    {
        if (string.IsNullOrWhiteSpace(State.NewVersion))
        {
            return;
        }

        string toRemoveSubscriptionId = null;
        if (!string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            toRemoveSubscriptionId = GetSubscriptionId(State.CurrentVersion);
            await StopBlockScanAsync(toRemoveSubscriptionId, State.CurrentVersion);
            State.VersionStatus.Remove(State.CurrentVersion);
        }

        State.CurrentVersion = State.NewVersion;
        State.NewVersion = null;
        await WriteStateAsync();
        
        await RemoveSubscriptionAsync(toRemoveSubscriptionId);
    }

    public async Task<VersionStatus> GetVersionStatusAsync(string version)
    {
        return State.VersionStatus[version];
    }

    public async Task StartAsync(string version)
    {
        State.VersionStatus[version] = VersionStatus.Started;
        await WriteStateAsync();
    }
    
    public async Task PauseAsync(string version)
    {
        State.VersionStatus[version] = VersionStatus.Paused;
        await WriteStateAsync();
    }

    public async Task StopAsync(string version)
    {
        if (State.CurrentVersion == version)
        {
            State.CurrentVersion = null;
        }
        else if (State.NewVersion == version)
        {
            State.NewVersion = null;
        }
        else
        {
            return;
        }
        
        State.VersionStatus.Remove(version);

        var toRemoveSubscriptionId = GetSubscriptionId(version);;
        await StopBlockScanAsync(toRemoveSubscriptionId, version);

        await WriteStateAsync();
        
        await RemoveSubscriptionAsync(toRemoveSubscriptionId);
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
    
    private string GetSubscriptionId(string version)
    {
        return GrainIdHelper.GenerateGrainId(this.GetPrimaryKeyString(), version);
    }
    
    private async Task StopBlockScanAsync(string subscriptionId, string version)
    {
        var subscriptionGrain = GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId);
        var subscription = await subscriptionGrain.GetSubscriptionAsync();
        foreach (var item in subscription.Items)
        {
            var id = GrainIdHelper.GenerateGrainId(item.Key, this.GetPrimaryKeyString(), version);
            await GrainFactory.GetGrain<IBlockScanGrain>(id).StopAsync();
        }
    }
    
    private async Task RemoveSubscriptionAsync(string subscriptionId)
    {
        if (!string.IsNullOrWhiteSpace(subscriptionId))
        {
            await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).RemoveAsync();
        }
    }
}