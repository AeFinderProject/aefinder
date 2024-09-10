using System.Collections.Concurrent;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.BlockScan;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.State.Subscriptions;
using AeFinder.Subscriptions;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using SubscriptionInfo = AeFinder.Grains.State.Subscriptions.SubscriptionInfo;

namespace AeFinder.Grains.Grain.Subscriptions;

public class AppSubscriptionGrain : AeFinderGrain<AppSubscriptionState>, IAppSubscriptionGrain
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<AppSubscriptionGrain> _logger;

    public AppSubscriptionGrain(IDistributedEventBus distributedEventBus, ILogger<AppSubscriptionGrain> logger)
    {
        _distributedEventBus = distributedEventBus;
        _logger = logger;
    }

    public async Task<AddSubscriptionDto> AddSubscriptionAsync(SubscriptionManifest subscriptionManifest, byte[] code)
    {
        var addSubscriptionDto = new AddSubscriptionDto();
        var newVersion = Guid.NewGuid().ToString("N");

        await ReadStateAsync();
        await BeginChangingStateAsync();

        var appSubscriptionCreateEto = new AppSubscriptionCreateEto();
        appSubscriptionCreateEto.AppId = this.GetPrimaryKeyString();
        
        if (State.CurrentVersion == null)
        {
            State.CurrentVersion = newVersion;
            await _distributedEventBus.PublishAsync(new AppCurrentVersionSetEto()
            {
                CurrentVersion = newVersion,
                AppId = this.GetPrimaryKeyString()
            });
            await GrainFactory.GetGrain<IAppGrain>(this.GetPrimaryKeyString()).SetStatusAsync(AppStatus.Deployed);
            
            appSubscriptionCreateEto.CurrentVersion = State.CurrentVersion;
        }
        else
        {
            if (State.PendingVersion != null)
            {
                //Stop current pending version
                addSubscriptionDto.StopVersion = State.PendingVersion;
                //Note: the state is re-read from the database in StopAsync(), so setting the state needs to be left behind
                await StopAsync(addSubscriptionDto.StopVersion);
            }

            State.PendingVersion = newVersion;
            appSubscriptionCreateEto.PendingVersion = State.PendingVersion;
        }
        
        State.SubscriptionInfos[newVersion] = new SubscriptionInfo
        {
            SubscriptionManifest = subscriptionManifest,
            Status = SubscriptionStatus.Initialized
        };

        await UpdateCodeAsync(newVersion, code);
        await WriteStateAsync();
        addSubscriptionDto.NewVersion = newVersion;
        
        //Publish app subscription create eto to background worker
        await _distributedEventBus.PublishAsync(appSubscriptionCreateEto);
        
        return addSubscriptionDto;
    }


    public async Task UpdateSubscriptionAsync(string version, SubscriptionManifest subscriptionManifest)
    {
        await ReadStateAsync();

        CheckVersion(version);

        State.SubscriptionInfos[version].SubscriptionManifest = subscriptionManifest;
        await WriteStateAsync();
        
        //Publish app subscription update eto to background worker
        await _distributedEventBus.PublishAsync(new AppSubscriptionUpdateEto()
        {
            AppId = this.GetPrimaryKeyString(),
            Version = version
        });
    }

    public async Task<SubscriptionManifest> GetSubscriptionAsync(string version)
    {
        await ReadStateAsync();

        CheckVersion(version);
        return State.SubscriptionInfos[version].SubscriptionManifest;
    }

    public async Task<AllSubscription> GetAllSubscriptionAsync()
    {
        await ReadStateAsync();

        var result = new AllSubscription();
        if (State.CurrentVersion != null)
        {
            result.CurrentVersion = new SubscriptionDetail
            {
                Version = State.CurrentVersion,
                Status = State.SubscriptionInfos[State.CurrentVersion].Status,
                SubscriptionManifest = State.SubscriptionInfos[State.CurrentVersion].SubscriptionManifest,
                ProcessingStatus = State.SubscriptionInfos[State.CurrentVersion].ProcessingStatus
            };
        }

        if (State.PendingVersion != null)
        {
            result.PendingVersion = new SubscriptionDetail
            {
                Version = State.PendingVersion,
                Status = State.SubscriptionInfos[State.PendingVersion].Status,
                SubscriptionManifest = State.SubscriptionInfos[State.PendingVersion].SubscriptionManifest,
                ProcessingStatus = State.SubscriptionInfos[State.PendingVersion].ProcessingStatus
            };
        }

        return result;
    }

    public async Task<byte[]> GetCodeAsync(string version)
    {
        var codeId = GetAppCodeId(version);
        return await GrainFactory.GetGrain<IAppCodeGrain>(codeId).GetCodeAsync();
    }

    public async Task UpdateCodeAsync(string version, byte[] code)
    {
        CheckVersion(version);
        
        var codeId = GetAppCodeId(version);
        await GrainFactory.GetGrain<IAppCodeGrain>(codeId).SetCodeAsync(code);
    }

    public async Task<bool> IsRunningAsync(string version, string chainId, string pushToken)
    {
        await ReadStateAsync();

        if (string.IsNullOrWhiteSpace(version) ||
            !State.SubscriptionInfos.TryGetValue(version, out var subscriptionInfo) ||
            subscriptionInfo.Status != SubscriptionStatus.Started)
        {
            return false;
        }

        if (!await GrainFactory
                .GetGrain<IBlockPusherInfoGrain>(
                    GrainIdHelper.GenerateBlockPusherGrainId(this.GetPrimaryKeyString(), version, chainId))
                .IsRunningAsync(pushToken))
        {
            return false;
        }

        return true;
    }

    public async Task UpgradeVersionAsync(string version)
    {
        await ReadStateAsync();
        
        if (State.PendingVersion == null || version != State.PendingVersion)
        {
            return;
        }
        
        await BeginChangingStateAsync();
        
        if (State.CurrentVersion != null)
        {
            await StopBlockPushAsync(State.CurrentVersion);
            
            //Publish app upgrade eto to background worker
            await _distributedEventBus.PublishAsync(new AppUpgradeEto()
            {
                AppId = this.GetPrimaryKeyString(),
                CurrentVersion = State.CurrentVersion,
                PendingVersion = State.PendingVersion,
                CurrentVersionChainIds = GetVersionSubscribedChainIds(State.CurrentVersion)
            });
            
            State.SubscriptionInfos.Remove(State.CurrentVersion);
        }
        
        _logger.LogInformation("Upgrade CurrentVersion from {currentVersion} to {pendingVersion}", State.CurrentVersion,
            State.PendingVersion);

        State.CurrentVersion = State.PendingVersion;
        await _distributedEventBus.PublishAsync(new AppCurrentVersionSetEto()
        {
            AppId = this.GetPrimaryKeyString(),
            CurrentVersion = State.PendingVersion
        });
        State.PendingVersion = null;
        await WriteStateAsync();

        await GrainFactory.GetGrain<IAppGrain>(this.GetPrimaryKeyString()).SetStatusAsync(AppStatus.Deployed);
    }

    public async Task<SubscriptionStatus> GetSubscriptionStatusAsync(string version)
    {
        await ReadStateAsync();
        return State.SubscriptionInfos[version].Status;
    }

    public async Task StartAsync(string version)
    {
        await ReadStateAsync();
        State.SubscriptionInfos[version].Status = SubscriptionStatus.Started;
        await ReSetProcessingStatusAsync(version);
        await WriteStateAsync();
        
        //Publish app subscription update eto to background worker
        await _distributedEventBus.PublishAsync(new AppSubscriptionUpdateEto()
        {
            AppId = this.GetPrimaryKeyString(),
            Version = version
        });
    }

    public async Task PauseAsync(string version)
    {
        await ReadStateAsync();
        State.SubscriptionInfos[version].Status = SubscriptionStatus.Paused;
        await WriteStateAsync();
        
        //Publish app subscription update eto to background worker
        await _distributedEventBus.PublishAsync(new AppSubscriptionUpdateEto()
        {
            AppId = this.GetPrimaryKeyString(),
            Version = version
        });
    }

    public async Task StopAsync(string version)
    {
        await ReadStateAsync();
        await BeginChangingStateAsync();
        
        if (version == State.CurrentVersion)
        {
            State.CurrentVersion = null;
        }
        else if (version == State.PendingVersion)
        {
            State.PendingVersion = null;
        }
        else
        {
            return;
        }

        await StopBlockPushAsync(version);

        var stopVersionChainIds = GetVersionSubscribedChainIds(version);
        _logger.LogInformation("Remove version {stopVersion} SubscriptionInfos", version);
        State.SubscriptionInfos.Remove(version);

        await WriteStateAsync();
        
        //Publish app stop eto to background worker
        await _distributedEventBus.PublishAsync(new AppStopEto()
        {
            AppId = this.GetPrimaryKeyString(),
            StopVersion = version,
            StopVersionChainIds = stopVersionChainIds
        });
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
    }

    private string GetAppCodeId(string version)
    {
        return GrainIdHelper.GenerateGetAppCodeGrainId(this.GetPrimaryKeyString(), version);
    }

    private async Task StopBlockPushAsync(string version)
    {
        var subscription = State.SubscriptionInfos[version].SubscriptionManifest;
        foreach (var item in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(this.GetPrimaryKeyString(), version, item.ChainId);
            await GrainFactory.GetGrain<IBlockPusherInfoGrain>(id).StopAsync();
        }
    }

    private void CheckVersion(string version)
    {
        if (version != State.CurrentVersion && version != State.PendingVersion)
        {
            throw new UserFriendlyException($"Invalid version: {version}");
        }
    }

    private List<string> GetVersionSubscribedChainIds(string version)
    {
        var currentVersionSubscriptionInfo = State.SubscriptionInfos[version];
        var currentVersionChainIds = new List<string>();
        foreach (var subscriptionItem in currentVersionSubscriptionInfo.SubscriptionManifest.SubscriptionItems)
        {
            currentVersionChainIds.Add(subscriptionItem.ChainId);
        }

        return currentVersionChainIds;
    }

    private async Task ReSetProcessingStatusAsync(string version)
    {
        if (State.SubscriptionInfos[version].ProcessingStatus == null)
        {
            State.SubscriptionInfos[version].ProcessingStatus =
                new ConcurrentDictionary<string, ProcessingStatus>();
        }
        var currentVersionSubscriptionInfo = State.SubscriptionInfos[version];
        foreach (var subscriptionItem in currentVersionSubscriptionInfo.SubscriptionManifest.SubscriptionItems)
        {
            var chainId = subscriptionItem.ChainId;
            State.SubscriptionInfos[version].ProcessingStatus.AddOrUpdate(chainId, ProcessingStatus.Running,
                (key, oldValue) => ProcessingStatus.Running);
        }
    }

    public async Task SetProcessingStatusAsync(string version, string chainId, ProcessingStatus processingStatus)
    {
        if (State.SubscriptionInfos[version].ProcessingStatus == null)
        {
            State.SubscriptionInfos[version].ProcessingStatus =
                new ConcurrentDictionary<string, ProcessingStatus>();
        }
        State.SubscriptionInfos[version].ProcessingStatus[chainId] = processingStatus;
        await WriteStateAsync();
    }
}