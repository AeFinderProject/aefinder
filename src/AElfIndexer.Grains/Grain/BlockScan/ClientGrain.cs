using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class ClientGrain : Grain<ClientState>, IClientGrain
{
    public async Task<string> AddSubscriptionInfoAsync(Subscription subscription)
    {
        var newVersion = Guid.NewGuid().ToString("N");
        State.VersionSubscriptions[newVersion] = new VersionSubscription
        {
            Subscription = subscription,
            VersionStatus = VersionStatus.Initialized
        };

        if (string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            State.CurrentVersion = newVersion;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(State.NewVersion))
            {
                foreach (var item in State.VersionSubscriptions[State.NewVersion].Subscription.Items)
                {
                    var id = GrainIdHelper.GenerateGrainId(item.Key, this.GetPrimaryKeyString(), State.NewVersion);
                    await GrainFactory.GetGrain<IBlockScanGrain>(id).StopAsync();
                }

                State.VersionSubscriptions.Remove(State.NewVersion);
            }

            State.NewVersion = newVersion;
        }

        await WriteStateAsync();
        return newVersion;
    }

    public async Task UpdateSubscriptionInfoAsync(string version, Subscription subscription)
    {
        if (version != State.NewVersion && version != State.CurrentVersion)
        {
            return;
        }

        State.VersionSubscriptions[version].Subscription = subscription;
        await WriteStateAsync();
    }

    public async Task<Subscription> GetSubscriptionAsync(string version)
    {
        return State.VersionSubscriptions[version].Subscription;
    }

    public async Task<SubscriptionInfoDto> GetAllSubscriptionAsync()
    {
        var result = new SubscriptionInfoDto();
        if (!string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            result.CurrentVersion = new SubscriptionInfoDetailDto
            {
                Version = State.CurrentVersion,
                SubscriptionInfos = State.VersionSubscriptions[State.CurrentVersion].Subscription
            };
        }
        
        if (!string.IsNullOrWhiteSpace(State.NewVersion))
        {
            result.NewVersion = new SubscriptionInfoDetailDto
            {
                Version = State.NewVersion,
                SubscriptionInfos = State.VersionSubscriptions[State.NewVersion].Subscription
            };
        }

        return result;
    }

    public async Task<bool> IsRunningAsync(string version, string chainId, string scanToken)
    {
        return !string.IsNullOrWhiteSpace(version) &&
               (version == State.NewVersion || version == State.CurrentVersion) &&
               State.VersionSubscriptions[version].VersionStatus == VersionStatus.Started &&
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

        if (!string.IsNullOrWhiteSpace(State.CurrentVersion))
        {
            foreach (var item in State.VersionSubscriptions[State.CurrentVersion].Subscription.Items)
            {
                var id = GrainIdHelper.GenerateGrainId(item.Key, this.GetPrimaryKeyString(), State.NewVersion);
                await GrainFactory.GetGrain<IBlockScanGrain>(id).StopAsync();
            }
            
            State.VersionSubscriptions.Remove(State.CurrentVersion);
        }

        State.CurrentVersion = State.NewVersion;
        State.NewVersion = null;
        await WriteStateAsync();
    }

    public async Task<VersionStatus> GetVersionStatusAsync(string version)
    {
        return State.VersionSubscriptions[version].VersionStatus;
    }

    public async Task StartAsync(string version)
    {
        State.VersionSubscriptions[version].VersionStatus = VersionStatus.Started;
        await WriteStateAsync();
    }
    
    public async Task PauseAsync(string version)
    {
        State.VersionSubscriptions[version].VersionStatus = VersionStatus.Paused;
        await WriteStateAsync();
    }

    public async Task StopAsync(string version)
    {
        State.VersionSubscriptions.Remove(version);
        if (State.CurrentVersion == version)
        {
            State.CurrentVersion = null;
        }
        
        if (State.NewVersion == version)
        {
            State.NewVersion = null;
        }
        
        foreach (var item in State.VersionSubscriptions[version].Subscription.Items)
        {
            var id = GrainIdHelper.GenerateGrainId(item.Key, this.GetPrimaryKeyString(), State.NewVersion);
            await GrainFactory.GetGrain<IBlockScanGrain>(id).StopAsync();
        }
        
        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
}