using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.App.Deploy;
using AeFinder.AppResources;
using AeFinder.Assets;
using AeFinder.BlockScan;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Options;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace AeFinder.Apps;

public class AppDeployServiceTests
{
    private const string AppId = "cleanup-app";
    private const string Version = "cleanup-version";
    private readonly Mock<IAppSubscriptionGrain> _subscriptionGrain = new();
    private readonly Mock<IBlockPusherManagerGrain> _managerGrain = new();
    private readonly Mock<IAppDeployManager> _deployManager = new();
    private readonly AppDeployService _service;
    private SubscriptionManifest _manifest;

    public AppDeployServiceTests()
    {
        var client = new Mock<IClusterClient>();
        client.Setup(x => x.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(AppId), null))
            .Returns(_subscriptionGrain.Object);
        client.Setup(x => x.GetGrain<IBlockPusherManagerGrain>(0L, null))
            .Returns(_managerGrain.Object);
        _subscriptionGrain.Setup(x => x.GetSubscriptionAsync(Version)).ReturnsAsync(() => _manifest);
        _subscriptionGrain.Setup(x => x.UpdateSubscriptionAsync(Version, It.IsAny<SubscriptionManifest>()))
            .Callback<string, SubscriptionManifest>((_, manifest) => _manifest = manifest)
            .Returns(Task.CompletedTask);

        _service = new AppDeployService(client.Object, new BlockScanAppService(client.Object),
            _deployManager.Object, Mock.Of<IOrganizationAppService>(), Options<AppDeployOptions>(),
            Options<CustomOrganizationOptions>(), Options<PodResourceOptions>(), Mock.Of<IAssetService>(),
            Mock.Of<IApiKeyService>(), Mock.Of<IUserAppService>(), Mock.Of<IAppEmailSender>(),
            Mock.Of<IAppResourceLimitProvider>(), Mock.Of<IAppResourceUsageService>())
        {
            LazyServiceProvider = new AbpLazyServiceProvider(new ServiceCollection().AddLogging().BuildServiceProvider())
        };
    }

    [Fact]
    public async Task Destroy_Should_Persist_Exact_Matching_Chains_Before_Pause_And_Reuse_Them_On_Retry()
    {
        var pusherIds = new Dictionary<string, HashSet<string>>
        {
            ["AELF"] = new() { PusherId("AELF"), PusherId("AELF", "another-version") },
            ["tDVV"] = new() { PusherId("tDVV") },
            ["tDVW"] = new() { PusherId("tDVW", "another-version"), "other-" + PusherId("tDVW") }
        };
        _managerGrain.Setup(x => x.GetAllBlockPusherIdsAsync()).ReturnsAsync(pusherIds);
        _managerGrain.Setup(x => x.RemoveBlockPusherAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((chain, id) =>
            {
                _manifest.SubscriptionItems.Select(x => x.ChainId).ShouldBe(new[] { "AELF", "tDVV" });
                pusherIds[chain].Remove(id);
            })
            .Returns(Task.CompletedTask);
        var attempts = 0;
        _deployManager.Setup(x => x.DestroyAppAsync(AppId, Version, It.IsAny<List<string>>()))
            .Returns<string, string, List<string>>((_, _, chains) =>
            {
                chains.ShouldBe(new[] { "AELF", "tDVV" });
                return ++attempts == 1
                    ? Task.FromException(new InvalidOperationException("Kubernetes unavailable"))
                    : Task.CompletedTask;
            });

        await Should.ThrowAsync<InvalidOperationException>(() => _service.DestroyAppAsync(AppId, Version));
        pusherIds["AELF"].ShouldNotContain(PusherId("AELF"));
        pusherIds["tDVV"].ShouldBeEmpty();
        await _service.DestroyAppAsync(AppId, Version);

        attempts.ShouldBe(2);
        _managerGrain.Verify(x => x.GetAllBlockPusherIdsAsync(), Times.Once);
        _subscriptionGrain.Verify(x => x.UpdateSubscriptionAsync(Version, It.IsAny<SubscriptionManifest>()), Times.Once);
    }

    [Fact]
    public async Task Destroy_Should_Preserve_Existing_Manifest()
    {
        _manifest = new SubscriptionManifest
        {
            SubscriptionItems = new() { new Subscription { ChainId = "AELF", StartBlockNumber = 123 } }
        };
        var original = _manifest;

        await _service.DestroyAppAsync(AppId, Version);

        _manifest.ShouldBeSameAs(original);
        _manifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(123);
        _managerGrain.Verify(x => x.GetAllBlockPusherIdsAsync(), Times.Never);
        _subscriptionGrain.Verify(x => x.UpdateSubscriptionAsync(Version, It.IsAny<SubscriptionManifest>()), Times.Never);
        _deployManager.Verify(x => x.DestroyAppAsync(AppId, Version,
            It.Is<List<string>>(chains => chains.SequenceEqual(new[] { "AELF" }))), Times.Once);
    }

    [Fact]
    public async Task Destroy_Should_Save_Empty_Manifest_When_No_Pushers_Are_Registered()
    {
        _managerGrain.Setup(x => x.GetAllBlockPusherIdsAsync())
            .ReturnsAsync(new Dictionary<string, HashSet<string>>());

        await _service.DestroyAppAsync(AppId, Version);

        _manifest.ShouldNotBeNull();
        _manifest.SubscriptionItems.ShouldBeEmpty();
        _deployManager.Verify(x => x.DestroyAppAsync(AppId, Version,
            It.Is<List<string>>(chains => chains.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task Destroy_Should_Not_Pause_Or_Destroy_When_Manager_Query_Fails()
    {
        _managerGrain.Setup(x => x.GetAllBlockPusherIdsAsync())
            .ThrowsAsync(new InvalidOperationException("Manager unavailable"));

        await Should.ThrowAsync<InvalidOperationException>(() => _service.DestroyAppAsync(AppId, Version));

        _manifest.ShouldBeNull();
        _subscriptionGrain.Verify(x => x.UpdateSubscriptionAsync(Version, It.IsAny<SubscriptionManifest>()), Times.Never);
        _subscriptionGrain.Verify(x => x.PauseAsync(It.IsAny<string>()), Times.Never);
        _deployManager.Verify(x => x.DestroyAppAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
    }

    private static string PusherId(string chain, string version = Version) =>
        GrainIdHelper.GenerateBlockPusherGrainId(AppId, version, chain);

    private static IOptionsSnapshot<T> Options<T>() where T : class, new() =>
        Mock.Of<IOptionsSnapshot<T>>(x => x.Value == new T());
}
