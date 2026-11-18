using System.Collections.Concurrent;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.BackgroundWorker.ScheduledTask;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker.Tests.ScheduledTasks;

public class AppRescanCheckWorkerExpiredVersionTests
{
    private readonly Mock<IEntityMappingRepository<AppInfoIndex, string>> _apps = new();
    private readonly Mock<IClusterClient> _client = new();
    private readonly Mock<IAppDeployService> _deploy = new();
    private readonly List<AppInfoIndex> _indexes = new();
    private readonly AppRescanCheckWorker _worker;

    public AppRescanCheckWorkerExpiredVersionTests()
    {
        _apps.Setup(x => x.GetQueryableAsync(null, default)).ReturnsAsync(() => _indexes.AsQueryable());
        var options = new Mock<IOptionsSnapshot<ScheduledTaskOptions>>();
        options.SetupGet(x => x.Value).Returns(new ScheduledTaskOptions());
        var provider = new ServiceCollection().BuildServiceProvider();
        _worker = new AppRescanCheckWorker(new AbpAsyncTimer(), _apps.Object,
            NullLogger<AppRescanCheckWorker>.Instance, _client.Object, Mock.Of<IObjectMapper>(),
            options.Object, _deploy.Object, provider.GetRequiredService<IServiceScopeFactory>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rescan_Should_Skip_Expired_ES_Version(bool hasOtherVersion)
    {
        var subscriptions = AddApp("expired", "old-version");
        subscriptions.Setup(x => x.GetAllSubscriptionAsync()).ReturnsAsync(new AllSubscription
        {
            CurrentVersion = hasOtherVersion ? new SubscriptionDetail { Version = "new-version" } : null
        });

        await _worker.ProcessRescanCheckAsync();

        _client.Verify(x => x.GetGrain<IAppSubscriptionProcessingStatusGrain>(It.IsAny<string>(), null), Times.Never);
        _deploy.Verify(x => x.RestartAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rescan_Should_Restart_Active_Failed_Version(bool pending)
    {
        AddActiveApp("active", "active-version", pending);

        await _worker.ProcessRescanCheckAsync();

        _deploy.Verify(x => x.RestartAppAsync("active", "active-version"), Times.Once);
    }

    [Fact]
    public async Task Rescan_Should_Not_Restart_Paused_Version()
    {
        AddActiveApp("paused", "paused-version", status: SubscriptionStatus.Paused);

        await _worker.ProcessRescanCheckAsync();

        _client.Verify(x => x.GetGrain<IAppSubscriptionProcessingStatusGrain>(It.IsAny<string>(), null), Times.Never);
        _deploy.Verify(x => x.RestartAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Rescan_Should_Continue_After_Grain_Failure()
    {
        var failed = AddApp("a-failed", "failed-version");
        failed.Setup(x => x.GetAllSubscriptionAsync()).ThrowsAsync(new InvalidOperationException("Orleans unavailable"));
        AddActiveApp("b-active", "active-version");

        await _worker.ProcessRescanCheckAsync();

        _deploy.Verify(x => x.RestartAppAsync("b-active", "active-version"), Times.Once);
        failed.Verify(x => x.GetAllSubscriptionAsync(), Times.Once);
    }

    [Fact]
    public async Task Rescan_Should_Continue_After_Restart_Failure_And_Retry_Next_Cycle()
    {
        AddActiveApp("a-failed", "failed-version");
        AddActiveApp("b-active", "active-version");
        _deploy.Setup(x => x.RestartAppAsync("a-failed", "failed-version"))
            .ThrowsAsync(new InvalidOperationException("Kubernetes unavailable"));

        await _worker.ProcessRescanCheckAsync();
        await _worker.ProcessRescanCheckAsync();

        _deploy.Verify(x => x.RestartAppAsync("a-failed", "failed-version"), Times.Exactly(2));
        _deploy.Verify(x => x.RestartAppAsync("b-active", "active-version"), Times.Exactly(2));
    }

    [Fact]
    public async Task Rescan_Should_Keep_Maximum_Restart_Attempts()
    {
        AddActiveApp("active", "active-version");

        for (var i = 0; i < 4; i++)
        {
            await _worker.ProcessRescanCheckAsync();
        }

        _deploy.Verify(x => x.RestartAppAsync("active", "active-version"), Times.Exactly(3));
    }

    private Mock<IAppSubscriptionGrain> AddApp(string appId, string version, bool pending = false)
    {
        _indexes.Add(new AppInfoIndex
        {
            AppId = appId, AppName = appId, Status = AppStatus.Deployed,
            Versions = pending ? new AppVersionInfo { PendingVersion = version } : new AppVersionInfo { CurrentVersion = version }
        });
        var subscriptions = new Mock<IAppSubscriptionGrain>();
        _client.Setup(x => x.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId), null))
            .Returns(subscriptions.Object);
        subscriptions.Setup(x => x.GetSubscriptionStatusAsync(version))
            .ThrowsAsync(new KeyNotFoundException(version));
        return subscriptions;
    }

    private void AddActiveApp(string appId, string version, bool pending = false,
        SubscriptionStatus status = SubscriptionStatus.Started)
    {
        var subscriptions = AddApp(appId, version, pending);
        var detail = new SubscriptionDetail { Version = version, Status = status };
        subscriptions.Setup(x => x.GetAllSubscriptionAsync()).ReturnsAsync(new AllSubscription
        {
            CurrentVersion = pending ? null : detail, PendingVersion = pending ? detail : null
        });
        var processing = new Mock<IAppSubscriptionProcessingStatusGrain>();
        processing.Setup(x => x.GetProcessingStatusAsync()).ReturnsAsync(new ConcurrentDictionary<string, ProcessingStatus>(
            new[] { new KeyValuePair<string, ProcessingStatus>("AELF", ProcessingStatus.Failed) }));
        _client.Setup(x => x.GetGrain<IAppSubscriptionProcessingStatusGrain>(
            GrainIdHelper.GenerateAppSubscriptionProcessingStatusGrainId(appId, version), null)).Returns(processing.Object);
    }
}
