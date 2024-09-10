using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.ScheduledTask;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Shouldly;

namespace AeFinder.BackgroundWorker.Tests.ScheduledTasks;

public class AppRescanCheckWorkerTests: AeFinderBackgroundWorkerCoreTestBase
{
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly AppRescanCheckWorker _appRescanCheckWorker;
    
    public AppRescanCheckWorkerTests()
    {
        _appInfoEntityMappingRepository = GetRequiredService<IEntityMappingRepository<AppInfoIndex, string>>();
        _appRescanCheckWorker = GetRequiredService<AppRescanCheckWorker>();
    }
    
    [Fact]
    public async Task AppRescanCheckWorker_Test()
    {
        var appId = "test_app";
        var chainId = "AELF";
        var subscriptionManifest = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 21
                }
            }
        };
        
        var appSubscriptionGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var version1 = (await appSubscriptionGrain.AddSubscriptionAsync(subscriptionManifest, new byte[] { })).NewVersion;
        
        var appInfoIndex = new AppInfoIndex()
        {
            OrganizationId="yuol123",
            OrganizationName = "A elf Team",
            AppId = "test_app",
            AppName = "test app",
            Status = AppStatus.Deployed,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,
            Versions = new AppVersionInfo()
            {
                CurrentVersion = version1
            }
        };
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);

        await appSubscriptionGrain.SetProcessingStatusAsync(version1, chainId, ProcessingStatus.Failed);
        var allSubscription = await appSubscriptionGrain.GetAllSubscriptionAsync();
        allSubscription.CurrentVersion.ShouldNotBeNull();
        allSubscription.CurrentVersion.ProcessingStatus[chainId].ShouldBe(ProcessingStatus.Failed);

        await _appRescanCheckWorker.ProcessRescanCheckAsync();
        
        await appSubscriptionGrain.SetProcessingStatusAsync(version1, chainId, ProcessingStatus.Failed);
        var subscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        subscriptions.CurrentVersion.ShouldNotBeNull();
        subscriptions.CurrentVersion.Status.ShouldBe(SubscriptionStatus.Paused);
    }
}