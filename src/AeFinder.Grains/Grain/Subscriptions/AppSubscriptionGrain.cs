using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.State.Subscriptions;
using AeFinder.Studio;
using Orleans;
using SubscriptionInfo = AeFinder.Grains.State.Subscriptions.SubscriptionInfo;

namespace AeFinder.Grains.Grain.Subscriptions;

public class AppSubscriptionGrain : Grain<AppSubscriptionState>, IAppSubscriptionGrain
{
    public async Task<string> AddSubscriptionAsync(SubscriptionManifest subscriptionManifest, byte[] code)
    {
        var dto = await AddSubscriptionV2Async(subscriptionManifest, code);
        return dto.NewVersion;
    }

    public async Task<AddSubscriptionDto> AddSubscriptionV2Async(SubscriptionManifest subscriptionManifest, byte[] code)
    {
        var addSubscriptionDto = new AddSubscriptionDto();
        var newVersion = Guid.NewGuid().ToString("N");

        await UpdateCodeAsync(newVersion, code);

        State.SubscriptionInfos[newVersion] = new SubscriptionInfo
        {
            SubscriptionManifest = subscriptionManifest,
            Status = SubscriptionStatus.Initialized
        };

        if (State.CurrentVersion == null)
        {
            State.CurrentVersion = newVersion;
        }
        else
        {
            if (State.NewVersion != null)
            {
                addSubscriptionDto.StopVersion = State.NewVersion;
                await StopBlockPushAsync(State.NewVersion);
                State.SubscriptionInfos.Remove(State.NewVersion);
            }

            State.NewVersion = newVersion;
        }

        await WriteStateAsync();
        addSubscriptionDto.NewVersion = newVersion;
        return addSubscriptionDto;
    }


    public async Task UpdateSubscriptionAsync(string version, SubscriptionManifest subscriptionManifest)
    {
        if (version != State.CurrentVersion && version != State.NewVersion)
        {
            return;
        }

        State.SubscriptionInfos[version].SubscriptionManifest = subscriptionManifest;
        await WriteStateAsync();
    }

    public async Task<SubscriptionManifest> GetSubscriptionAsync(string version)
    {
        return State.SubscriptionInfos[version].SubscriptionManifest;
    }

    public async Task<AllSubscription> GetAllSubscriptionAsync()
    {
        var result = new AllSubscription();
        if (State.CurrentVersion != null)
        {
            result.CurrentVersion = new SubscriptionDetail
            {
                Version = State.CurrentVersion,
                Status = State.SubscriptionInfos[State.CurrentVersion].Status,
                SubscriptionManifest = State.SubscriptionInfos[State.CurrentVersion].SubscriptionManifest
            };
        }

        if (State.NewVersion != null)
        {
            result.NewVersion = new SubscriptionDetail
            {
                Version = State.NewVersion,
                Status = State.SubscriptionInfos[State.NewVersion].Status,
                SubscriptionManifest = State.SubscriptionInfos[State.NewVersion].SubscriptionManifest
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
        var codeId = GetAppCodeId(version);
        await GrainFactory.GetGrain<IAppCodeGrain>(codeId).SetCodeAsync(code);
    }

    public async Task<bool> IsRunningAsync(string version, string chainId, string pushToken)
    {
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

    public async Task UpgradeVersionAsync()
    {
        if (State.NewVersion == null)
        {
            return;
        }

        if (State.CurrentVersion != null)
        {
            await StopBlockPushAsync(State.CurrentVersion);
            State.SubscriptionInfos.Remove(State.CurrentVersion);
        }

        State.CurrentVersion = State.NewVersion;
        State.NewVersion = null;
        await WriteStateAsync();
    }

    public Task<SubscriptionStatus> GetSubscriptionStatusAsync(string version)
    {
        return Task.FromResult(State.SubscriptionInfos[version].Status);
    }

    public async Task StartAsync(string version)
    {
        State.SubscriptionInfos[version].Status = SubscriptionStatus.Started;
        await WriteStateAsync();
    }

    public async Task PauseAsync(string version)
    {
        State.SubscriptionInfos[version].Status = SubscriptionStatus.Paused;
        await WriteStateAsync();
    }

    public async Task StopAsync(string version)
    {
        if (version == State.CurrentVersion)
        {
            State.CurrentVersion = null;
        }
        else if (version == State.NewVersion)
        {
            State.NewVersion = null;
        }
        else
        {
            return;
        }

        await StopBlockPushAsync(version);
        State.SubscriptionInfos.Remove(version);

        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
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
}