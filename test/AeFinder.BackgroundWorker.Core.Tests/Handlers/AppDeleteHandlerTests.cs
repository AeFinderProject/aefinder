using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.EventHandler;
using AeFinder.BlockScan;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans;
using Shouldly;
using Volo.Abp.Application.Dtos;

namespace AeFinder.BackgroundWorker.Tests.Handlers;

public class AppDeleteHandlerTests
{
    private const string AppId = "delete-null-manifest";
    private const string Version = "cleanup-version";
    private readonly Mock<IAppSubscriptionGrain> _subscriptions = new();
    private readonly Mock<IAppDeployService> _deploy = new();
    private readonly Mock<IBlockScanAppService> _scan = new();
    private readonly Mock<IEntityMappingRepository<AppInfoIndex, string>> _appIndex = new();
    private readonly Mock<IEntityMappingRepository<OrganizationIndex, string>> _organizationIndex = new();
    private readonly AppDeleteHandler _handler;
    private readonly AllSubscription _versions = new();
    private readonly AppDeleteEto _event = new()
    {
        AppId = AppId,
        OrganizationId = Guid.NewGuid().ToString(),
        Status = AppStatus.Deleted,
        DeleteTime = DateTime.UtcNow
    };
    private readonly AppInfoIndex _app = new() { AppId = AppId };
    private readonly OrganizationIndex _organization = new() { AppIds = new() { AppId, "another-app" } };

    public AppDeleteHandlerTests()
    {
        var client = new Mock<IClusterClient>();
        client.Setup(x => x.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(AppId), null))
            .Returns(_subscriptions.Object);
        _subscriptions.Setup(x => x.GetAllSubscriptionAsync()).ReturnsAsync(_versions);
        var assets = new Mock<IAssetService>();
        assets.Setup(x => x.GetListAsync(It.IsAny<Guid>(), It.IsAny<GetAssetInput>()))
            .ReturnsAsync(new PagedResultDto<AssetDto>());
        _appIndex.Setup(x => x.GetAsync(AppId, null, default)).ReturnsAsync(_app);
        _organizationIndex.Setup(x => x.GetAsync(_event.OrganizationId, null, default)).ReturnsAsync(_organization);

        using var provider = new ServiceCollection().AddSingleton(_deploy.Object).BuildServiceProvider();
        _handler = ActivatorUtilities.CreateInstance<AppDeleteHandler>(provider,
            NullLogger<AppDeleteHandler>.Instance, client.Object, _scan.Object, assets.Object,
            _appIndex.Object, _organizationIndex.Object);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_Should_Destroy_Null_Manifest_Version_Before_Stop(bool pending)
    {
        var version = SetVersion(pending);
        var operations = new List<string>();
        _deploy.Setup(x => x.DestroyAppAsync(AppId, Version)).Callback(() =>
        {
            operations.Add("destroy");
            version.SubscriptionManifest = new SubscriptionManifest();
        }).Returns(Task.CompletedTask);
        _scan.Setup(x => x.StopAsync(AppId, Version)).Callback(() =>
        {
            version.SubscriptionManifest.ShouldNotBeNull();
            operations.Add("stop");
        }).Returns(Task.CompletedTask);

        await _handler.HandleEventAsync(_event);

        operations.ShouldBe(new[] { "destroy", "stop" });
        _app.Status.ShouldBe(AppStatus.Deleted);
        _app.DeleteTime.ShouldBe(_event.DeleteTime);
        _organization.AppIds.ShouldBe(new[] { "another-app" });
        _appIndex.Verify(x => x.AddOrUpdateAsync(_app, null, default), Times.Once);
        _organizationIndex.Verify(x => x.UpdateAsync(_organization, null, default), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_Should_Not_Stop_Or_Update_Indexes_When_Destroy_Fails(bool pending)
    {
        SetVersion(pending);
        var error = new InvalidOperationException("Kubernetes unavailable");
        _deploy.Setup(x => x.DestroyAppAsync(AppId, Version)).ThrowsAsync(error);

        var actual = await Should.ThrowAsync<InvalidOperationException>(() => _handler.HandleEventAsync(_event));

        actual.ShouldBeSameAs(error);
        _scan.Verify(x => x.StopAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _appIndex.Verify(x => x.AddOrUpdateAsync(It.IsAny<AppInfoIndex>(), null, default), Times.Never);
        _organizationIndex.Verify(x => x.UpdateAsync(It.IsAny<OrganizationIndex>(), null, default), Times.Never);
    }

    [Fact]
    public async Task Delete_Should_Update_Indexes_Without_Cleanup_When_No_Versions_Exist()
    {
        await _handler.HandleEventAsync(_event);

        _deploy.Verify(x => x.DestroyAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _scan.Verify(x => x.StopAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _app.Status.ShouldBe(AppStatus.Deleted);
        _organization.AppIds.ShouldNotContain(AppId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_Should_Destroy_Before_Stop_When_Manifest_Already_Exists(bool pending)
    {
        var detail = SetVersion(pending);
        var manifest = new SubscriptionManifest
        {
            SubscriptionItems = new() { new Subscription { ChainId = "AELF", StartBlockNumber = 123 } }
        };
        detail.SubscriptionManifest = manifest;
        var operations = new List<string>();
        _deploy.Setup(x => x.DestroyAppAsync(AppId, Version)).Callback(() => operations.Add("destroy"))
            .Returns(Task.CompletedTask);
        _scan.Setup(x => x.StopAsync(AppId, Version)).Callback(() => operations.Add("stop"))
            .Returns(Task.CompletedTask);

        await _handler.HandleEventAsync(_event);

        operations.ShouldBe(new[] { "destroy", "stop" });
        detail.SubscriptionManifest.ShouldBeSameAs(manifest);
        detail.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(123);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_Should_Retry_Destroy_After_Manifest_Was_Saved_Before_Failure(bool pending)
    {
        var detail = SetVersion(pending);
        var savedManifest = new SubscriptionManifest
        {
            SubscriptionItems = new() { new Subscription { ChainId = "AELF" } }
        };
        var attempts = 0;
        var operations = new List<string>();
        _deploy.Setup(x => x.DestroyAppAsync(AppId, Version)).Returns(() =>
        {
            operations.Add("destroy");
            detail.SubscriptionManifest = savedManifest;
            return ++attempts == 1
                ? Task.FromException(new InvalidOperationException("Kubernetes unavailable"))
                : Task.CompletedTask;
        });
        _scan.Setup(x => x.StopAsync(AppId, Version)).Callback(() => operations.Add("stop"))
            .Returns(Task.CompletedTask);

        await Should.ThrowAsync<InvalidOperationException>(() => _handler.HandleEventAsync(_event));
        detail.SubscriptionManifest.ShouldBeSameAs(savedManifest);
        _scan.Verify(x => x.StopAsync(AppId, Version), Times.Never);

        await _handler.HandleEventAsync(_event);

        operations.ShouldBe(new[] { "destroy", "destroy", "stop" });
        _deploy.Verify(x => x.DestroyAppAsync(AppId, Version), Times.Exactly(2));
        _scan.Verify(x => x.StopAsync(AppId, Version), Times.Once);
    }

    [Fact]
    public async Task Delete_Should_Destroy_And_Stop_Both_Current_And_Pending_Versions()
    {
        _versions.CurrentVersion = new SubscriptionDetail { Version = "current" };
        _versions.PendingVersion = new SubscriptionDetail { Version = "pending" };
        var operations = new List<string>();
        _deploy.Setup(x => x.DestroyAppAsync(AppId, It.IsAny<string>()))
            .Callback<string, string>((_, version) => operations.Add("destroy:" + version))
            .Returns(Task.CompletedTask);
        _scan.Setup(x => x.StopAsync(AppId, It.IsAny<string>()))
            .Callback<string, string>((_, version) => operations.Add("stop:" + version))
            .Returns(Task.CompletedTask);

        await _handler.HandleEventAsync(_event);

        operations.ShouldBe(new[] { "destroy:current", "stop:current", "destroy:pending", "stop:pending" });
    }

    private SubscriptionDetail SetVersion(bool pending)
    {
        var detail = new SubscriptionDetail { Version = Version, SubscriptionManifest = null! };
        if (pending)
        {
            _versions.PendingVersion = detail;
        }
        else
        {
            _versions.CurrentVersion = detail;
        }
        return detail;
    }
}
