using AElfIndexer.Grains.Grain.BlockScanExecution;
using AElfIndexer.Grains.Grain.Subscriptions;
using AElfIndexer.Grains.State.ScanApps;
using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.ScanApps;

public class ScanAppGrain : Grain<ScanAppState>, IScanAppGrain
{
    public async Task<string> AddSubscriptionAsync(Subscription subscription)
    {
        var newVersion = Guid.NewGuid().ToString("N");
        var subscriptionId = GetSubscriptionId(newVersion);
        await GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId).SetSubscriptionAsync(subscription);

        string toRemoveSubscriptionId = null;
        if (State.CurrentVersion == null)
        {
            State.CurrentVersion = new ScanAppVersion
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
                await StopBlockScanAsync(toRemoveSubscriptionId, State.NewVersion.Version);
            }
            State.NewVersion = new ScanAppVersion
            {
                Version = newVersion,
                Status = SubscriptionStatus.Initialized
            };
        }

        await WriteStateAsync();

        await RemoveSubscriptionAsync(toRemoveSubscriptionId);
        
        return newVersion;
    }

    public async Task UpdateSubscriptionAsync(string version, Subscription subscription)
    {
        if (!IsNewVersion(version) && !IsCurrentVersion(version))
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

    public async Task<AllSubscription> GetAllSubscriptionAsync()
    {
        var result = new AllSubscription();
        if (State.CurrentVersion != null)
        {
            result.CurrentVersion = new SubscriptionDetail
            {
                Version = State.CurrentVersion.Version,
                Status = State.CurrentVersion.Status,
                Subscription = await GrainFactory
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
                Subscription = await GrainFactory
                    .GetGrain<ISubscriptionGrain>(GetSubscriptionId(State.NewVersion.Version)).GetSubscriptionAsync()
            };
        }

        return result;
    }

    public async Task<byte[]> GetCodeAsync(string version)
    {
        var codeId = GetScanAppCodeId(version);
        return await GrainFactory.GetGrain<IScanAppCodeGrain>(codeId).GetCodeAsync();
    }

    public async Task<bool> IsRunningAsync(string version, string chainId, string scanToken)
    {
        var scanAppVersion = GetScanAppVersion(version);
        if (scanAppVersion == null)
        {
            return false;
        }
        if(scanAppVersion.Status != SubscriptionStatus.Started)
        {
            return false;
        }

        if (!await GrainFactory
                .GetGrain<IBlockScanGrain>(
                    GrainIdHelper.GenerateBlockScanGrainId(this.GetPrimaryKeyString(), version, chainId))
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
            await StopBlockScanAsync(toRemoveSubscriptionId, State.CurrentVersion.Version);
        }

        State.CurrentVersion = new ScanAppVersion
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
        return Task.FromResult(GetScanAppVersion(version).Status);
    }

    public async Task StartAsync(string version)
    {
        var scanAppVersion = GetScanAppVersion(version);
        scanAppVersion.Status = SubscriptionStatus.Started;
        await WriteStateAsync();
    }
    
    public async Task PauseAsync(string version)
    {
        var scanAppVersion = GetScanAppVersion(version);
        scanAppVersion.Status = SubscriptionStatus.Paused;
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
        return GrainIdHelper.GenerateSubscriptionGrainId(this.GetPrimaryKeyString(), version);
    }
    
    private string GetScanAppCodeId(string version)
    {
        return GrainIdHelper.GenerateGetScanAppCodeGrainId(this.GetPrimaryKeyString(), version);
    }
    
    private async Task StopBlockScanAsync(string subscriptionId, string version)
    {
        var subscriptionGrain = GrainFactory.GetGrain<ISubscriptionGrain>(subscriptionId);
        var subscription = await subscriptionGrain.GetSubscriptionAsync();
        foreach (var item in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockScanGrainId(this.GetPrimaryKeyString(), version, item.ChainId);
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
    
    private bool IsCurrentVersion(string version)
    {
        return State.CurrentVersion != null && State.CurrentVersion.Version == version;
    }
    
    private bool IsNewVersion(string version)
    {
        return State.NewVersion != null && State.NewVersion.Version == version;
    }
    
    private ScanAppVersion GetScanAppVersion(string version)
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