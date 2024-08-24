using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.BlockScan;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AeFinder.Subscriptions;

public class SubscriptionAppServiceTests : AeFinderApplicationOrleansTestBase
{
    private readonly ISubscriptionAppService _subscriptionAppService;
    private readonly IAppService _appService;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _subscriptionIndexRepository;

    public SubscriptionAppServiceTests()
    {
        _subscriptionAppService = GetRequiredService<ISubscriptionAppService>();
        _appService = GetRequiredService<IAppService>();
        _subscriptionIndexRepository = GetRequiredService<IEntityMappingRepository<AppSubscriptionIndex, string>>();
    }

    [Fact]
    public async Task SubscriptionTest()
    {
        var appId = (await _appService.CreateAsync(new CreateAppDto { AppName = "AppId" })).AppId;
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

        var version1 = await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1], null);

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
        subscription.PendingVersion.ShouldBeNull();
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
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1], null));

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
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1], null));

        subscriptionInput = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
            }
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1], null));

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
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1], null));
    }
    
    [Fact]
    public async Task UpdateSubscriptionTest()
    {
        var appId = (await _appService.CreateAsync(new CreateAppDto { AppName = "AppId" })).AppId;
        
        var subscriptionInfo1 = new SubscriptionManifestDto()
        {
            SubscriptionItems=new List<SubscriptionDto>()
            {
                new SubscriptionDto()
                {
                    ChainId = "AELF",
                    OnlyConfirmed = false,
                    StartBlockNumber = 999,
                    Transactions = new List<TransactionConditionDto>()
                    {
                        new TransactionConditionDto()
                        {
                            To = "ToAddress",
                            MethodNames = new List<string>()
                            {
                                "Method1",
                                "Method2"
                            }
                        }
                    },
                    LogEvents = new List<LogEventConditionDto>()
                    {
                        new LogEventConditionDto()
                        {
                            ContractAddress = "TokenAddress1",
                            EventNames = new List<string>()
                            {
                                "Transfer",
                                "Burned"
                            }
                        }
                    }
                }
            }
        };
        var dll = System.Text.Encoding.UTF8.GetBytes("Program codes");
        var version1 =
            await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInfo1, dll, null);
        
        var subscription = await _subscriptionAppService.GetSubscriptionManifestAsync(appId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems.Count.ShouldBe(1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(999);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].LogEvents[0].EventNames.Count.ShouldBe(2);
        subscription.PendingVersion.ShouldBeNull();

        var subscriptionInfo2 = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new SubscriptionDto()
                {
                    ChainId = "AELF",
                    OnlyConfirmed = false,
                    StartBlockNumber = 999,
                    Transactions = new List<TransactionConditionDto>()
                    {
                        new TransactionConditionDto()
                        {
                            To = "ToAddress",
                            MethodNames = new List<string>()
                            {
                                "Method1",
                                "Method2"
                            }
                        }
                    },
                    LogEvents = new List<LogEventConditionDto>()
                    {
                        new LogEventConditionDto()
                        {
                            ContractAddress = "TokenAddress1",
                            EventNames = new List<string>()
                            {
                                "Transfer",
                                "Burned",
                                "Created",
                                "TransactionChargedFee"
                            }
                        }
                    }
                }
            }
        };
        await _subscriptionAppService.UpdateSubscriptionManifestAsync(appId, version1, subscriptionInfo2);
        var subscription2 = await _subscriptionAppService.GetSubscriptionManifestAsync(appId);
        subscription2.CurrentVersion.Version.ShouldBe(version1);
        subscription2.CurrentVersion.SubscriptionManifest.SubscriptionItems.Count.ShouldBe(1);
        subscription2.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].LogEvents[0].EventNames.Count.ShouldBe(4);
        
        var subscriptionInfo3 = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new SubscriptionDto()
                {
                    ChainId = "AELF",
                    OnlyConfirmed = false,
                    StartBlockNumber = 999,
                    Transactions = new List<TransactionConditionDto>()
                    {
                        new TransactionConditionDto()
                        {
                            To = "ToAddress",
                            MethodNames = new List<string>()
                            {
                                "Method1",
                                "Method2",
                                "Method3"
                            }
                        }
                    },
                    LogEvents = new List<LogEventConditionDto>()
                    {
                        new LogEventConditionDto()
                        {
                            ContractAddress = "TokenAddress1",
                            EventNames = new List<string>()
                            {
                                "Transfer",
                                "Burned",
                                "Created",
                                "TransactionChargedFee"
                            }
                        }
                    }
                }
            }
        };
        await _subscriptionAppService.UpdateSubscriptionManifestAsync(appId, version1, subscriptionInfo3);
        var subscription3 = await _subscriptionAppService.GetSubscriptionManifestAsync(appId);
        subscription3.CurrentVersion.Version.ShouldBe(version1);
        subscription3.CurrentVersion.SubscriptionManifest.SubscriptionItems.Count.ShouldBe(1);
        subscription3.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].Transactions[0].MethodNames.Count.ShouldBe(3);
        subscription3.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].LogEvents[0].EventNames.Count.ShouldBe(4);
    }

    [Fact]
    public async Task SubscriptionIndex_Test()
    {
        await _subscriptionIndexRepository.AddAsync(new AppSubscriptionIndex
        {
            AppId = "AppId",
            Version = "Version1",
            SubscriptionStatus = SubscriptionStatus.Initialized,
            SubscriptionManifest = new SubscriptionManifestInfo
            {
                SubscriptionItems = new List<SubscriptionInfo>()
                {
                    new SubscriptionInfo()
                    {
                        ChainId = "AELF",
                        OnlyConfirmed = true,
                        StartBlockNumber = 999,
                        TransactionConditions = new List<TransactionConditionInfo>()
                        {
                            new TransactionConditionInfo()
                            {
                                To = "ToAddress",
                                MethodNames = new List<string>()
                                {
                                    "Method1"
                                }
                            }
                        },
                        LogEventConditions = new List<LogEventConditionInfo>()
                        {
                            new LogEventConditionInfo()
                            {
                                ContractAddress = "TokenAddress1",
                                EventNames = new List<string>()
                                {
                                    "Transfer"
                                }
                            }
                        }
                    }
                }
            }
        });
        
        await _subscriptionIndexRepository.AddAsync(new AppSubscriptionIndex
        {
            AppId = "AppId",
            Version = "Version2",
            SubscriptionStatus = SubscriptionStatus.Paused,
            SubscriptionManifest = new SubscriptionManifestInfo
            {
                SubscriptionItems = new List<SubscriptionInfo>()
                {
                    new SubscriptionInfo()
                    {
                        ChainId = "tDVV",
                        OnlyConfirmed = false,
                        StartBlockNumber = 10,
                        TransactionConditions = new List<TransactionConditionInfo>()
                        {
                            new TransactionConditionInfo()
                            {
                                To = "ToAddress",
                                MethodNames = new List<string>()
                                {
                                    "Method"
                                }
                            },
                            new TransactionConditionInfo()
                            {
                                To = "ToAddress2",
                                MethodNames = new List<string>()
                                {
                                    "Method2"
                                }
                            }
                        },
                        LogEventConditions = new List<LogEventConditionInfo>()
                        {
                            new LogEventConditionInfo()
                            {
                                ContractAddress = "TokenAddress",
                                EventNames = new List<string>()
                                {
                                    "Transfer"
                                }
                            },
                            new LogEventConditionInfo()
                            {
                                ContractAddress = "TokenAddress2",
                                EventNames = new List<string>()
                                {
                                    "Transfer"
                                }
                            }
                        }
                    }
                }
            }
        });

        var subscription = await _subscriptionAppService.GetSubscriptionManifestIndexAsync("AppId");
        subscription.Count.ShouldBe(2);
        var subscriptionVersion1 = subscription.First(o => o.Version == "Version1");
        subscriptionVersion1.AppId.ShouldBe("AppId");
        subscriptionVersion1.SubscriptionStatus.ShouldBe(SubscriptionStatus.Initialized);
        subscriptionVersion1.SubscriptionManifest.SubscriptionItems.Count().ShouldBe(1);
        subscriptionVersion1.SubscriptionManifest.SubscriptionItems[0].ChainId.ShouldBe("AELF");
        subscriptionVersion1.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(999);
        subscriptionVersion1.SubscriptionManifest.SubscriptionItems[0].OnlyConfirmed.ShouldBeTrue();
        subscriptionVersion1.SubscriptionManifest.SubscriptionItems[0].LogEvents.Count().ShouldBe(1);
        subscriptionVersion1.SubscriptionManifest.SubscriptionItems[0].Transactions.Count().ShouldBe(1);
        var subscriptionVersion2 = subscription.First(o => o.Version == "Version2");
        subscriptionVersion2.AppId.ShouldBe("AppId");
        subscriptionVersion2.SubscriptionStatus.ShouldBe(SubscriptionStatus.Paused);
        subscriptionVersion2.SubscriptionManifest.SubscriptionItems.Count().ShouldBe(1);
        subscriptionVersion2.SubscriptionManifest.SubscriptionItems[0].ChainId.ShouldBe("tDVV");
        subscriptionVersion2.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(10);
        subscriptionVersion2.SubscriptionManifest.SubscriptionItems[0].OnlyConfirmed.ShouldBeFalse();
        subscriptionVersion2.SubscriptionManifest.SubscriptionItems[0].LogEvents.Count().ShouldBe(2);
        subscriptionVersion2.SubscriptionManifest.SubscriptionItems[0].Transactions.Count().ShouldBe(2);
    }
}