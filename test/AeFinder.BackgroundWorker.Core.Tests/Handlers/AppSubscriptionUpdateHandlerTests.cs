using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.BackgroundWorker.EventHandler;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.BackgroundWorker.Tests.Handlers;

public class AppSubscriptionUpdateHandlerTests
{
    private const string AppId = "subscription-update";
    private const string Version = "event-version";
    private readonly Mock<IAppSubscriptionGrain> _subscriptions = new();
    private readonly Mock<IEntityMappingRepository<AppSubscriptionIndex, string>> _index = new();
    private readonly Mock<IObjectMapper> _mapper = new();
    private readonly AllSubscription _versions = new();
    private readonly AppSubscriptionUpdateHandler _handler;
    private readonly AppSubscriptionUpdateEto _event = new() { AppId = AppId, Version = Version };

    public AppSubscriptionUpdateHandlerTests()
    {
        var client = new Mock<IClusterClient>();
        client.Setup(x => x.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(AppId), null))
            .Returns(_subscriptions.Object);
        _subscriptions.Setup(x => x.GetAllSubscriptionAsync()).ReturnsAsync(_versions);
        _subscriptions.Setup(x => x.GetSubscriptionAsync(Version))
            .ThrowsAsync(new InvalidOperationException("Invalid version: " + Version));
        var provider = new ServiceCollection().AddLogging().AddSingleton(_mapper.Object).BuildServiceProvider();
        _handler = new AppSubscriptionUpdateHandler(client.Object, _index.Object)
        {
            LazyServiceProvider = new AbpLazyServiceProvider(provider)
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_Should_Ignore_Expired_Version(bool hasOtherVersion)
    {
        if (hasOtherVersion)
        {
            _versions.CurrentVersion = new SubscriptionDetail { Version = "new-version" };
        }

        await _handler.HandleEventAsync(_event);

        _index.Verify(x => x.AddOrUpdateAsync(It.IsAny<AppSubscriptionIndex>(), null, default), Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Update_Should_Sync_Active_Version_From_One_Snapshot(bool pending, bool nullManifest)
    {
        var manifest = nullManifest ? null : new SubscriptionManifest();
        var detail = new SubscriptionDetail
        {
            Version = Version, Status = SubscriptionStatus.Paused, SubscriptionManifest = manifest!
        };
        if (pending) _versions.PendingVersion = detail;
        else _versions.CurrentVersion = detail;
        var mapped = nullManifest ? null : new SubscriptionManifestInfo();
        _mapper.Setup(x => x.Map<SubscriptionManifest, SubscriptionManifestInfo>(manifest!)).Returns(mapped!);

        await _handler.HandleEventAsync(_event);

        _index.Verify(x => x.AddOrUpdateAsync(It.Is<AppSubscriptionIndex>(i =>
            i.AppId == AppId && i.Version == Version && i.SubscriptionStatus == SubscriptionStatus.Paused &&
            i.SubscriptionManifest == mapped), null, default), Times.Once);
        _subscriptions.Verify(x => x.GetSubscriptionAsync(It.IsAny<string>()), Times.Never);
        _subscriptions.Verify(x => x.GetSubscriptionStatusAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Update_Should_Propagate_Grain_Failure()
    {
        var error = new InvalidOperationException("Orleans unavailable");
        _subscriptions.Setup(x => x.GetAllSubscriptionAsync()).ThrowsAsync(error);

        var actual = await Should.ThrowAsync<InvalidOperationException>(() => _handler.HandleEventAsync(_event));

        actual.ShouldBeSameAs(error);
        _index.Verify(x => x.AddOrUpdateAsync(It.IsAny<AppSubscriptionIndex>(), null, default), Times.Never);
    }

    [Fact]
    public async Task Update_Should_Propagate_Index_Failure()
    {
        _versions.CurrentVersion = new SubscriptionDetail { Version = Version };
        var error = new InvalidOperationException("Elasticsearch unavailable");
        _index.Setup(x => x.AddOrUpdateAsync(It.IsAny<AppSubscriptionIndex>(), null, default)).ThrowsAsync(error);

        var actual = await Should.ThrowAsync<InvalidOperationException>(() => _handler.HandleEventAsync(_event));

        actual.ShouldBeSameAs(error);
    }
}
