using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AeFinder.Subscriptions;

public class SubscriptionAppServiceTests : AeFinderApplicationOrleansTestBase
{
    private readonly ISubscriptionAppService _subscriptionAppService;

    public SubscriptionAppServiceTests()
    {
        _subscriptionAppService = GetRequiredService<ISubscriptionAppService>();
    }

    [Fact]
    public async Task SubscriptionTest()
    {
        var appId = "AppId";
        var chainId = "AELF";
        var subscriptionInput = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = chainId,
                    StartBlockNumber = 100,
                    OnlyConfirmed = true,
                    LogEvents = new List<LogEventConditionDto>
                    {
                        new LogEventConditionDto
                        {
                            ContractAddress = "TokenAddress",
                            EventNames = { "TokenCreated" }
                        }
                    },
                    Transactions = new List<TransactionConditionDto>
                    {
                        new TransactionConditionDto
                        {
                            To = "CrossChainAddress",
                            MethodNames = { "CrossChainReceived" }
                        }
                    }
                }
            }
        };

        var version1 = await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1]);

        var subscription = await _subscriptionAppService.GetSubscriptionManifestAsync(appId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].ChainId.ShouldBe(chainId);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(100);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].OnlyConfirmed.ShouldBeTrue();
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].LogEvents.Count.ShouldBe(1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].LogEvents[0].ContractAddress
            .ShouldBe("TokenAddress");
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].LogEvents[0].EventNames
            .ShouldContain("TokenCreated");
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].Transactions.Count.ShouldBe(1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].Transactions[0].To
            .ShouldBe("CrossChainAddress");
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].Transactions[0].MethodNames
            .ShouldContain("CrossChainReceived");
        subscription.NewVersion.ShouldBeNull();
    }

    [Fact]
    public async Task Subscription_ValidateFailed_Test()
    {
        var appId = "AppId";
        var subscriptionInput = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = "abc",
                    StartBlockNumber = 100,
                    OnlyConfirmed = true,
                }
            }
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1]));

        subscriptionInput = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = "AELF",
                    StartBlockNumber = 0,
                    OnlyConfirmed = true,
                }
            }
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1]));

        subscriptionInput = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
            }
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1]));

        subscriptionInput = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = "AELF",
                    StartBlockNumber = 1,
                    OnlyConfirmed = true,
                },
                new()
                {
                    ChainId = "AELF",
                    StartBlockNumber = 10,
                    OnlyConfirmed = true,
                }
            }
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1]));
    }
}