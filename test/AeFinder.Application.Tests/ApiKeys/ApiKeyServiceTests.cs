using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using Orleans;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;
using Volo.Abp.Validation;
using Xunit;

namespace AeFinder.ApiKeys;

public class ApiKeyServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IApiKeySnapshotService _apiKeySnapshotService;
    private readonly IClusterClient _clusterClient;
    private readonly IClock _clock;
    private readonly IApiKeyTrafficProvider _apiKeyTrafficProvider;

    public ApiKeyServiceTests()
    {
        _apiKeyService = GetRequiredService<IApiKeyService>();
        _apiKeySnapshotService = GetRequiredService<IApiKeySnapshotService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _clock = GetRequiredService<IClock>();
        _apiKeyTrafficProvider = GetRequiredService<IApiKeyTrafficProvider>();
    }

    [Fact]
    public async Task Create_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey",
            IsEnableSpendingLimit = true,
            SpendingLimitUsdt = 100
        };

        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);
        apiKey.Id.ShouldNotBe(Guid.Empty);
        apiKey.OrganizationId.ShouldBe(orgId);
        apiKey.Key.ShouldNotBeNull();
        apiKey.Name.ShouldBe(createInput.Name);
        apiKey.IsEnableSpendingLimit.ShouldBe(createInput.IsEnableSpendingLimit);
        apiKey.SpendingLimitUsdt.ShouldBe(createInput.SpendingLimitUsdt);
        apiKey.IsActive.ShouldBeTrue();
        apiKey.AuthorisedApis.Count.ShouldBe(0);
        apiKey.AuthorisedDomains.Count.ShouldBe(0);
        apiKey.AuthorisedAeIndexers.Count.ShouldBe(0);
        apiKey.TotalQuery.ShouldBe(0);
        apiKey.PeriodQuery.ShouldBe(0);
        apiKey.IsDeleted.ShouldBeFalse();

        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.Id.ShouldBe(apiKey.Id);
        apiKeyInfo.OrganizationId.ShouldBe(orgId);
        apiKeyInfo.Key.ShouldBe(apiKey.Key);
        apiKeyInfo.Name.ShouldBe(createInput.Name);
        apiKeyInfo.IsEnableSpendingLimit.ShouldBe(createInput.IsEnableSpendingLimit);
        apiKeyInfo.SpendingLimitUsdt.ShouldBe(createInput.SpendingLimitUsdt);
        apiKeyInfo.AuthorisedApis.Count.ShouldBe(0);
        apiKeyInfo.AuthorisedDomains.Count.ShouldBe(0);
        apiKeyInfo.AuthorisedAeIndexers.Count.ShouldBe(0);
        apiKeyInfo.TotalQuery.ShouldBe(0);
        apiKeyInfo.IsDeleted.ShouldBeFalse();

        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(orgId));
        var apiKeySummary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        apiKeySummary.OrganizationId.ShouldBe(orgId);
        apiKeySummary.ApiKeyCount.ShouldBe(1);
        
        createInput = new CreateApiKeyInput
        {
            Name = "ApiKey",
            IsEnableSpendingLimit = true,
            SpendingLimitUsdt = -1
        };
        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _apiKeyService.CreateApiKeyAsync(orgId, createInput));
        
        createInput = new CreateApiKeyInput
        {
            Name = "",
        };
        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _apiKeyService.CreateApiKeyAsync(orgId, createInput));
        
        createInput = new CreateApiKeyInput
        {
            Name = "1234567890123456789012345678901",
        };
        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _apiKeyService.CreateApiKeyAsync(orgId, createInput));

        for (int i = 0; i < 9; i++)
        {
            createInput = new CreateApiKeyInput
            {
                Name = "ApiKey"+i
            };

            await _apiKeyService.CreateApiKeyAsync(orgId, createInput); 
        }
        
        createInput = new CreateApiKeyInput
        {
            Name = "ApiKey10"
        };
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.CreateApiKeyAsync(orgId, createInput));
    }

    [Fact]
    public async Task Update_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey",
            IsEnableSpendingLimit = true,
            SpendingLimitUsdt = 100
        };

        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);
        apiKey.Name.ShouldBe(createInput.Name);
        apiKey.IsEnableSpendingLimit.ShouldBe(createInput.IsEnableSpendingLimit);
        apiKey.SpendingLimitUsdt.ShouldBe(createInput.SpendingLimitUsdt);

        var updateInput = new UpdateApiKeyInput
        {
            Name = "NewApiKey"
        };

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.UpdateApiKeyAsync(Guid.NewGuid(), apiKey.Id, updateInput));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.UpdateApiKeyAsync( orgId, Guid.NewGuid(), updateInput));
        
        await _apiKeyService.UpdateApiKeyAsync(orgId, apiKey.Id, updateInput);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.Name.ShouldBe("NewApiKey");
        apiKeyInfo.IsEnableSpendingLimit.ShouldBe(createInput.IsEnableSpendingLimit);
        apiKeyInfo.SpendingLimitUsdt.ShouldBe(createInput.SpendingLimitUsdt);
        
        updateInput = new UpdateApiKeyInput
        {
            IsEnableSpendingLimit = false,
            SpendingLimitUsdt = 200
        };
        await _apiKeyService.UpdateApiKeyAsync(orgId, apiKey.Id, updateInput);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.Name.ShouldBe("NewApiKey");
        apiKeyInfo.IsEnableSpendingLimit.ShouldBe(updateInput.IsEnableSpendingLimit.Value);
        apiKeyInfo.SpendingLimitUsdt.ShouldBe(updateInput.SpendingLimitUsdt.Value);
        
        updateInput = new UpdateApiKeyInput
        {
            Name = "NewApiKey",
            SpendingLimitUsdt = -1
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _apiKeyService.UpdateApiKeyAsync(orgId, apiKey.Id, updateInput));
        
        updateInput = new UpdateApiKeyInput
        {
            Name = ""
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _apiKeyService.UpdateApiKeyAsync(orgId, apiKey.Id, updateInput));
        
        updateInput = new UpdateApiKeyInput
        {
            Name = "1234567890123456789012345678901"
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
            await _apiKeyService.UpdateApiKeyAsync(orgId, apiKey.Id, updateInput));
    }

    [Fact]
    public async Task RegenerateKey_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey"
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.RegenerateKeyAsync(Guid.NewGuid(), apiKey.Id));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.RegenerateKeyAsync( orgId, Guid.NewGuid()));

        await _apiKeyService.RegenerateKeyAsync(orgId, apiKey.Id);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.Key.ShouldNotBeNull();
        apiKeyInfo.Key.ShouldNotBe(apiKey.Key);
    }
    
    [Fact]
    public async Task Delete_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey"
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.DeleteApiKeyAsync(Guid.NewGuid(), apiKey.Id));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.DeleteApiKeyAsync( orgId, Guid.NewGuid()));

        await _apiKeyService.DeleteApiKeyAsync(orgId, apiKey.Id);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.IsDeleted.ShouldBeTrue();
        
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(orgId));
        var apiKeySummary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        apiKeySummary.OrganizationId.ShouldBe(orgId);
        apiKeySummary.ApiKeyCount.ShouldBe(0);
    }
    
    [Fact]
    public async Task SetAuthorisedAeIndexers_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey"
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);

        var setInput = new SetAuthorisedAeIndexerInput
        {
            AppIds = { "App1", "App2" }
        };
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.SetAuthorisedAeIndexersAsync(Guid.NewGuid(), apiKey.Id, setInput));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.SetAuthorisedAeIndexersAsync( orgId, Guid.NewGuid(), setInput));

        await _apiKeyService.SetAuthorisedAeIndexersAsync(orgId, apiKey.Id, setInput);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.AuthorisedAeIndexers.Count.ShouldBe(2);
        apiKeyInfo.AuthorisedAeIndexers["App1"].AppId.ShouldBe("App1");
        apiKeyInfo.AuthorisedAeIndexers["App2"].AppId.ShouldBe("App2");

        var deleteInput = new SetAuthorisedAeIndexerInput
        {
            AppIds = { "App1" }
        };
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.DeleteAuthorisedAeIndexersAsync(Guid.NewGuid(), apiKey.Id, deleteInput));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.DeleteAuthorisedAeIndexersAsync( orgId, Guid.NewGuid(), deleteInput));

        await _apiKeyService.DeleteAuthorisedAeIndexersAsync(orgId, apiKey.Id, deleteInput);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.AuthorisedAeIndexers.Count.ShouldBe(1);
        apiKeyInfo.AuthorisedAeIndexers["App2"].AppId.ShouldBe("App2");
    }
    
    [Fact]
    public async Task SetAuthorisedDomains_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey"
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);

        var setInput = new SetAuthorisedDomainInput
        {
            Domains = { "www.abc.com", "bcd.cn" }
        };
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.SetAuthorisedDomainsAsync(Guid.NewGuid(), apiKey.Id, setInput));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.SetAuthorisedDomainsAsync( orgId, Guid.NewGuid(), setInput));

        await _apiKeyService.SetAuthorisedDomainsAsync(orgId, apiKey.Id, setInput);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.AuthorisedDomains.Count.ShouldBe(2);
        apiKeyInfo.AuthorisedDomains.ShouldContain("www.abc.com");
        apiKeyInfo.AuthorisedDomains.ShouldContain("bcd.cn");

        var deleteInput = new SetAuthorisedDomainInput
        {
            Domains = { "www.abc.com" }
        };
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.DeleteAuthorisedDomainsAsync(Guid.NewGuid(), apiKey.Id, deleteInput));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.DeleteAuthorisedDomainsAsync( orgId, Guid.NewGuid(), deleteInput));

        await _apiKeyService.DeleteAuthorisedDomainsAsync(orgId, apiKey.Id, deleteInput);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.AuthorisedDomains.Count.ShouldBe(1);
        apiKeyInfo.AuthorisedDomains.ShouldContain("bcd.cn");
    }
    
    [Fact]
    public async Task SetAuthorisedApisAsync_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey"
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);

        var setInput = new SetAuthorisedApiInput
        {
            Apis = new Dictionary<BasicApi, bool>
            {
                { BasicApi.Block, false },
                { BasicApi.Transaction, true },
                { BasicApi.LogEvent, true }
            }
        };
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.SetAuthorisedApisAsync(Guid.NewGuid(), apiKey.Id, setInput));
        
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            await _apiKeyService.SetAuthorisedApisAsync( orgId, Guid.NewGuid(), setInput));

        await _apiKeyService.SetAuthorisedApisAsync(orgId, apiKey.Id, setInput);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.AuthorisedApis.Count.ShouldBe(2);
        apiKeyInfo.AuthorisedApis.ShouldContain(BasicApi.Transaction);
        apiKeyInfo.AuthorisedApis.ShouldContain(BasicApi.LogEvent);

        setInput = new SetAuthorisedApiInput
        {
            Apis = new Dictionary<BasicApi, bool>
            {
                { BasicApi.Block, true },
                { BasicApi.Transaction, false }
            }
        };
        await _apiKeyService.SetAuthorisedApisAsync(orgId, apiKey.Id, setInput);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.AuthorisedApis.Count.ShouldBe(2);
        apiKeyInfo.AuthorisedApis.ShouldContain(BasicApi.Block);
        apiKeyInfo.AuthorisedApis.ShouldContain(BasicApi.LogEvent);
    }

    [Fact]
    public async Task GetApiKeySummary_Test()
    {
        var orgId = Guid.NewGuid();
        var apiKeySummaryChangedEto = new ApiKeySummaryChangedEto
        {
            TotalQuery = 100,
            ApiKeyCount = 2,
            OrganizationId = orgId,
            LastQueryTime = _clock.Now,
            QueryLimit = 200,
            Id = GrainIdHelper.GenerateApiKeySummaryGrainId(orgId)
        };

        await _apiKeyService.AddOrUpdateApiKeySummaryIndexAsync(apiKeySummaryChangedEto);

        var summary = await _apiKeyService.GetApiKeySummaryAsync(orgId);
        summary.OrganizationId.ShouldBe(orgId);
        summary.TotalQuery.ShouldBe(apiKeySummaryChangedEto.TotalQuery);
        summary.ApiKeyCount.ShouldBe(apiKeySummaryChangedEto.ApiKeyCount);
        summary.LastQueryTime.ShouldBe(apiKeySummaryChangedEto.LastQueryTime);
        summary.QueryLimit.ShouldBe(apiKeySummaryChangedEto.QueryLimit);
        summary.Query.ShouldBe(0);

        var apiKeySummarySnapshotChangedEto = new ApiKeySummarySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, _clock.Now.AddMonths(-1)),
            Query = 10,
            Time = _clock.Now.AddMonths(-1).ToMonthDate(),
            Type = SnapshotType.Monthly
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySummarySnapshotIndexAsync(apiKeySummarySnapshotChangedEto);
        
        summary = await _apiKeyService.GetApiKeySummaryAsync(orgId);
        summary.OrganizationId.ShouldBe(orgId);
        summary.TotalQuery.ShouldBe(apiKeySummaryChangedEto.TotalQuery);
        summary.ApiKeyCount.ShouldBe(apiKeySummaryChangedEto.ApiKeyCount);
        summary.LastQueryTime.ShouldBe(apiKeySummaryChangedEto.LastQueryTime);
        summary.QueryLimit.ShouldBe(apiKeySummaryChangedEto.QueryLimit);
        summary.Query.ShouldBe(0);
        
        var query = await _apiKeyService.GetMonthQueryCountAsync(orgId, _clock.Now);
        query.ShouldBe(0);
        
        apiKeySummarySnapshotChangedEto = new ApiKeySummarySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, _clock.Now),
            Query = 20,
            Time = _clock.Now.ToMonthDate(),
            Type = SnapshotType.Monthly
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySummarySnapshotIndexAsync(apiKeySummarySnapshotChangedEto);
        summary = await _apiKeyService.GetApiKeySummaryAsync(orgId);
        summary.OrganizationId.ShouldBe(orgId);
        summary.TotalQuery.ShouldBe(apiKeySummaryChangedEto.TotalQuery);
        summary.ApiKeyCount.ShouldBe(apiKeySummaryChangedEto.ApiKeyCount);
        summary.LastQueryTime.ShouldBe(apiKeySummaryChangedEto.LastQueryTime);
        summary.QueryLimit.ShouldBe(apiKeySummaryChangedEto.QueryLimit);
        summary.Query.ShouldBe(20);

        query = await _apiKeyService.GetMonthQueryCountAsync(orgId, _clock.Now);
        query.ShouldBe(20);
    }

    [Fact]
    public async Task GetApiKey_Test()
    {
        var id = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var apiKeyChangedEto = new ApiKeyChangedEto
        {
            Id = id,
            OrganizationId = orgId,
            Name = "ApiKey",
            Key = Guid.NewGuid().ToString("N"),
            IsEnableSpendingLimit = true,
            SpendingLimitUsdt = 1,
            AuthorisedAeIndexers = new Dictionary<string, AppInfoImmutableEto>
            {
                { "app1", new AppInfoImmutableEto { AppId = "app1", AppName = "appName1" } }
            },
            AuthorisedDomains = new HashSet<string> { "www.abc.com", "bc.cn" },
            AuthorisedApis = new HashSet<BasicApi> { BasicApi.Transaction, BasicApi.LogEvent },
            LastQueryTime = _clock.Now,
            IsDeleted = false,
            TotalQuery = 200,
            CreateTime = _clock.Now,
            UpdateTime = _clock.Now
        };
        await _apiKeyService.AddOrUpdateApiKeyIndexAsync(apiKeyChangedEto);

        var apiKeyDto = await _apiKeyService.GetApiKeyAsync(orgId, id);
        apiKeyDto.Id.ShouldBe(id);
        apiKeyDto.OrganizationId.ShouldBe(orgId);
        apiKeyDto.Name.ShouldBe(apiKeyChangedEto.Name);
        apiKeyDto.Key.ShouldBe(apiKeyChangedEto.Key);
        apiKeyDto.IsEnableSpendingLimit.ShouldBe(apiKeyChangedEto.IsEnableSpendingLimit);
        apiKeyDto.SpendingLimitUsdt.ShouldBe(apiKeyChangedEto.SpendingLimitUsdt);
        apiKeyDto.AuthorisedAeIndexers.Count.ShouldBe(1);
        apiKeyDto.AuthorisedAeIndexers[0].AppId.ShouldBe("app1");
        apiKeyDto.AuthorisedAeIndexers[0].AppName.ShouldBe("appName1");
        apiKeyDto.AuthorisedDomains.Count.ShouldBe(2);
        apiKeyDto.AuthorisedDomains.ShouldContain("www.abc.com");
        apiKeyDto.AuthorisedDomains.ShouldContain("bc.cn");
        apiKeyDto.AuthorisedApis.Count.ShouldBe(2);
        apiKeyDto.AuthorisedApis.ShouldContain(BasicApi.Transaction);
        apiKeyDto.AuthorisedApis.ShouldContain(BasicApi.LogEvent);
        apiKeyDto.LastQueryTime.ShouldBe(apiKeyChangedEto.LastQueryTime);
        apiKeyDto.IsDeleted.ShouldBe(apiKeyChangedEto.IsDeleted);
        apiKeyDto.TotalQuery.ShouldBe(apiKeyChangedEto.TotalQuery);
        apiKeyDto.CreateTime.ShouldBe(apiKeyChangedEto.CreateTime);
        apiKeyDto.UpdateTime.ShouldBe(apiKeyChangedEto.UpdateTime);
        apiKeyDto.PeriodQuery.ShouldBe(0);
        apiKeyDto.IsActive.ShouldBe(true);

        var apiKeyDtos = await _apiKeyService.GetApiKeysAsync(orgId, new GetApiKeyInput());
        apiKeyDtos.TotalCount.ShouldBe(1);
        apiKeyDtos.Items.Count.ShouldBe(1);
        apiKeyDtos.Items[0].Id.ShouldBe(id);
        apiKeyDtos.Items[0].PeriodQuery.ShouldBe(0);
        apiKeyDtos.Items[0].IsActive.ShouldBe(true);
        
        var apiKeySnapshotChangedEto = new ApiKeySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(orgId, _clock.Now),
            Query = 10,
            Time = _clock.Now.ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = id
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySnapshotIndexAsync(apiKeySnapshotChangedEto);
        
        apiKeyDto = await _apiKeyService.GetApiKeyAsync(orgId, id);
        apiKeyDto.PeriodQuery.ShouldBe(10);
        apiKeyDto.IsActive.ShouldBe(true);
        
        apiKeyDtos = await _apiKeyService.GetApiKeysAsync(orgId, new GetApiKeyInput());
        apiKeyDtos.Items[0].PeriodQuery.ShouldBe(10);
        apiKeyDtos.Items[0].IsActive.ShouldBe(true);
        
        apiKeySnapshotChangedEto = new ApiKeySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(orgId, _clock.Now),
            Query = 25000,
            Time = _clock.Now.ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = id
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySnapshotIndexAsync(apiKeySnapshotChangedEto);
        
        apiKeyDto = await _apiKeyService.GetApiKeyAsync(orgId, id);
        apiKeyDto.PeriodQuery.ShouldBe(25000);
        apiKeyDto.IsActive.ShouldBe(false);
        
        apiKeyDtos = await _apiKeyService.GetApiKeysAsync(orgId, new GetApiKeyInput());
        apiKeyDtos.Items[0].PeriodQuery.ShouldBe(25000);
        apiKeyDtos.Items[0].IsActive.ShouldBe(false);

        apiKeyChangedEto.IsEnableSpendingLimit = false;
        await _apiKeyService.AddOrUpdateApiKeyIndexAsync(apiKeyChangedEto);
        
        apiKeyDto = await _apiKeyService.GetApiKeyAsync(orgId, id);
        apiKeyDto.PeriodQuery.ShouldBe(25000);
        apiKeyDto.IsActive.ShouldBe(true);
        
        apiKeyDtos = await _apiKeyService.GetApiKeysAsync(orgId, new GetApiKeyInput());
        apiKeyDtos.Items[0].PeriodQuery.ShouldBe(25000);
        apiKeyDtos.Items[0].IsActive.ShouldBe(true);
    }

    [Fact]
    public async Task GetApiKeyQueryAeIndexers_Test()
    {
        var orgId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var apiKeyQueryAeIndexerChangedEto1 = new ApiKeyQueryAeIndexerChangedEto
        {
            Id = GrainIdHelper.GenerateApiKeyQueryAeIndexerGrainId(apiKeyId, "app1"),
            OrganizationId = orgId,
            AppId = "app1",
            AppName = "appName1",
            TotalQuery = 100,
            LastQueryTime = _clock.Now,
            ApiKeyId = apiKeyId
        };
        await _apiKeyService.AddOrUpdateApiKeyQueryAeIndexerIndexAsync(apiKeyQueryAeIndexerChangedEto1);
        
        var apiKeyQueryAeIndexerChangedEto2 = new ApiKeyQueryAeIndexerChangedEto
        {
            Id = GrainIdHelper.GenerateApiKeyQueryAeIndexerGrainId(apiKeyId, "app2"),
            OrganizationId = orgId,
            AppId = "app2",
            AppName = "appName2",
            TotalQuery = 200,
            LastQueryTime = _clock.Now,
            ApiKeyId = apiKeyId
        };
        await _apiKeyService.AddOrUpdateApiKeyQueryAeIndexerIndexAsync(apiKeyQueryAeIndexerChangedEto2);

        var list = await _apiKeyService.GetApiKeyQueryAeIndexersAsync(orgId, apiKeyId,
            new GetApiKeyQueryAeIndexerInput());
        list.TotalCount.ShouldBe(2);
        list.Items.Count.ShouldBe(2);
        list.Items[0].OrganizationId.ShouldBe(orgId);
        list.Items[0].ApiKeyId.ShouldBe(apiKeyId);
        list.Items[0].AppId.ShouldBe(apiKeyQueryAeIndexerChangedEto1.AppId);
        list.Items[0].AppName.ShouldBe(apiKeyQueryAeIndexerChangedEto1.AppName);
        list.Items[0].TotalQuery.ShouldBe(apiKeyQueryAeIndexerChangedEto1.TotalQuery);
        list.Items[0].LastQueryTime.ShouldBe(apiKeyQueryAeIndexerChangedEto1.LastQueryTime);
        list.Items[1].OrganizationId.ShouldBe(orgId);
        list.Items[1].ApiKeyId.ShouldBe(apiKeyId);
        list.Items[1].AppId.ShouldBe(apiKeyQueryAeIndexerChangedEto2.AppId);
        list.Items[1].AppName.ShouldBe(apiKeyQueryAeIndexerChangedEto2.AppName);
        list.Items[1].TotalQuery.ShouldBe(apiKeyQueryAeIndexerChangedEto2.TotalQuery);
        list.Items[1].LastQueryTime.ShouldBe(apiKeyQueryAeIndexerChangedEto2.LastQueryTime);

        list = await _apiKeyService.GetApiKeyQueryAeIndexersAsync(orgId, apiKeyId,
            new GetApiKeyQueryAeIndexerInput
            {
                AppId = "app2"
            });
        list.TotalCount.ShouldBe(1);
        list.Items.Count.ShouldBe(1);
        list.Items[0].AppId.ShouldBe(apiKeyQueryAeIndexerChangedEto2.AppId);
    }
    
    [Fact]
    public async Task GetApiKeyQueryApis_Test()
    {
        var orgId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var apiKeyQueryBasicApiChangedEto1 = new ApiKeyQueryBasicApiChangedEto
        {
            Id = GrainIdHelper.GenerateApiKeyQueryBasicApiGrainId(apiKeyId, BasicApi.Block),
            OrganizationId = orgId,
            TotalQuery = 100,
            LastQueryTime = _clock.Now,
            ApiKeyId = apiKeyId,
            Api = BasicApi.Block
        };
        await _apiKeyService.AddOrUpdateApiKeyQueryBasicApiIndexAsync(apiKeyQueryBasicApiChangedEto1);
        
        var apiKeyQueryBasicApiChangedEto2 = new ApiKeyQueryBasicApiChangedEto
        {
            Id = GrainIdHelper.GenerateApiKeyQueryBasicApiGrainId(apiKeyId, BasicApi.Transaction),
            OrganizationId = orgId,
            TotalQuery = 200,
            LastQueryTime = _clock.Now,
            ApiKeyId = apiKeyId,
            Api = BasicApi.Transaction
        };
        await _apiKeyService.AddOrUpdateApiKeyQueryBasicApiIndexAsync(apiKeyQueryBasicApiChangedEto2);

        var list = await _apiKeyService.GetApiKeyQueryApisAsync(orgId, apiKeyId,
            new GetApiKeyQueryApiInput());
        list.TotalCount.ShouldBe(2);
        list.Items.Count.ShouldBe(2);
        list.Items[0].OrganizationId.ShouldBe(orgId);
        list.Items[0].ApiKeyId.ShouldBe(apiKeyId);
        list.Items[0].Api.ShouldBe(BasicApi.Block);
        list.Items[0].TotalQuery.ShouldBe(apiKeyQueryBasicApiChangedEto1.TotalQuery);
        list.Items[0].LastQueryTime.ShouldBe(apiKeyQueryBasicApiChangedEto1.LastQueryTime);
        list.Items[1].OrganizationId.ShouldBe(orgId);
        list.Items[1].ApiKeyId.ShouldBe(apiKeyId);
        list.Items[1].Api.ShouldBe(BasicApi.Transaction);
        list.Items[1].TotalQuery.ShouldBe(apiKeyQueryBasicApiChangedEto2.TotalQuery);
        list.Items[1].LastQueryTime.ShouldBe(apiKeyQueryBasicApiChangedEto2.LastQueryTime);

        list = await _apiKeyService.GetApiKeyQueryApisAsync(orgId, apiKeyId,
            new GetApiKeyQueryApiInput
            {
                Api = BasicApi.Transaction
            });
        list.TotalCount.ShouldBe(1);
        list.Items.Count.ShouldBe(1);
        list.Items[0].Api.ShouldBe(apiKeyQueryBasicApiChangedEto2.Api);
    }

    [Fact]
    public async Task IncreaseQueryAeIndexerCount_Test()
    {
        var orgId = Guid.NewGuid();
        await _apiKeyService.SetQueryLimitAsync(orgId, 10);
        
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(orgId));
        var summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.QueryLimit.ShouldBe(10);
        
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey"
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();

        await _apiKeyService.UpdateApiKeyInfoCacheAsync(apiKeyInfo);

        var time = _clock.Now.AddMonths(-1);
        await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "aaa.com", time);
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(0);
        
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(1);
        summary.LastQueryTime.ShouldBe(time);

        var apiSummaryMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, time));
        var query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        var apiSummaryDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryDailySnapshotGrainId(orgId, time));
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(1);
        apiKeyInfo.LastQueryTime.ShouldBe(time);
        
        var apiMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(apiKey.Id, time));
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        var apiDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyDailySnapshotGrainId(apiKey.Id, time));
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);

        var apiQueryAeIndexerGrain =
            _clusterClient.GetGrain<IApiKeyQueryAeIndexerGrain>(
                GrainIdHelper.GenerateApiKeyQueryAeIndexerGrainId(apiKey.Id, "app1"));
        var apiQueryAeIndexerInfo = await apiQueryAeIndexerGrain.GetAsync();
        apiQueryAeIndexerInfo.AppId.ShouldBe("app1");
        apiQueryAeIndexerInfo.ApiKeyId.ShouldBe(apiKey.Id);
        apiQueryAeIndexerInfo.TotalQuery.ShouldBe(1);
        apiQueryAeIndexerInfo.LastQueryTime.ShouldBe(time);

        var apiQueryAeIndexerMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(apiKey.Id, "app1", time));
        query = await apiQueryAeIndexerMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        var apiQueryAeIndexerDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryAeIndexerDailySnapshotGrainId(apiKey.Id, "app1", time));
        query = await apiQueryAeIndexerDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);

        for (int i = 0; i < 10; i++)
        {
            await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "aaa.com", _clock.Now.AddMonths(-1));
        }
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(10);
        
        query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(10);
        
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiQueryAeIndexerInfo = await apiQueryAeIndexerGrain.GetAsync();
        apiQueryAeIndexerInfo.TotalQuery.ShouldBe(10);
        
        query = await apiQueryAeIndexerMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiQueryAeIndexerDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "aaa.com", _clock.Now.AddMonths(-1));
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(10);
        
        query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(10);
        
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiQueryAeIndexerInfo = await apiQueryAeIndexerGrain.GetAsync();
        apiQueryAeIndexerInfo.TotalQuery.ShouldBe(10);
        
        query = await apiQueryAeIndexerMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiQueryAeIndexerDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);

        time = _clock.Now.AddMinutes(-5);
        await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "aaa.com", time);
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(11);
        summary.LastQueryTime.ShouldBe(time);

        apiSummaryMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, time));
        query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiSummaryDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryDailySnapshotGrainId(orgId, time));
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(11);
        apiKeyInfo.LastQueryTime.ShouldBe(time);
        
        apiMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(apiKey.Id, time));
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyDailySnapshotGrainId(apiKey.Id, time));
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);

        apiQueryAeIndexerGrain =
            _clusterClient.GetGrain<IApiKeyQueryAeIndexerGrain>(
                GrainIdHelper.GenerateApiKeyQueryAeIndexerGrainId(apiKey.Id, "app1"));
        apiQueryAeIndexerInfo = await apiQueryAeIndexerGrain.GetAsync();
        apiQueryAeIndexerInfo.AppId.ShouldBe("app1");
        apiQueryAeIndexerInfo.ApiKeyId.ShouldBe(apiKey.Id);
        apiQueryAeIndexerInfo.TotalQuery.ShouldBe(11);
        apiQueryAeIndexerInfo.LastQueryTime.ShouldBe(time);

        apiQueryAeIndexerMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(apiKey.Id, "app1", time));
        query = await apiQueryAeIndexerMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiQueryAeIndexerDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryAeIndexerDailySnapshotGrainId(apiKey.Id, "app1", time));
        query = await apiQueryAeIndexerDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        await _apiKeyService.IncreaseQueryAeIndexerCountAsync("App", "app1", "aaa.com", time);
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(11);
        
        await _apiKeyService.IncreaseQueryAeIndexerCountAsync("app", "app1", "aaa.com", time);
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(11);
    }
    
    [Fact]
    public async Task IncreaseQueryAeIndexerCount_Verify_Test()
    {
        var orgId = Guid.NewGuid();
        await _apiKeyService.SetQueryLimitAsync(orgId, 0);

        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey",
            IsEnableSpendingLimit = true,
            SpendingLimitUsdt = AeFinderApplicationConsts.ApiKeyQueryPrice * 3
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);

        await _apiKeyService.SetAuthorisedDomainsAsync(orgId, apiKey.Id, new SetAuthorisedDomainInput
        {
            Domains = { "aaa.com", "*.bbb.com" }
        });

        await _apiKeyService.SetAuthorisedAeIndexersAsync(orgId, apiKey.Id, new SetAuthorisedAeIndexerInput
        {
            AppIds = { "app1" }
        });
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();

        await _apiKeyService.UpdateApiKeyInfoCacheAsync(apiKeyInfo);
        
        var time = _clock.Now.AddMinutes(-5);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "aaa.com", time));
        exception.Message.ShouldBe("Api key query times insufficient.");
        
        await _apiKeyService.SetQueryLimitAsync(orgId, 10);
        await _apiKeyService.UpdateApiKeySummaryLimitCacheAsync(orgId, 10);
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app2", "aaa.com", time));
        exception.Message.ShouldBe("Unauthorized AeIndexer.");
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "a.aaa.com", time));
        exception.Message.ShouldBe("Unauthorized domain.");

        await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "aaa.com", time);
        await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "bbb.com", time);
        await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "b.bbb.com", time);

        await _apiKeyTrafficProvider.FlushAsync();
        
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(orgId));
        var summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(3);

        await _apiKeyService.UpdateApiKeyUsedCacheAsync(apiKey.Id, time, 3);
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryAeIndexerCountAsync(apiKey.Key, "app1", "aaa.com", time));
        exception.Message.ShouldBe("Api key unavailable.");
    }
    
    [Fact]
    public async Task IncreaseQueryApiCount_Test()
    {
        var orgId = Guid.NewGuid();
        await _apiKeyService.SetQueryLimitAsync(orgId, 10);
        
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(orgId));
        var summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.QueryLimit.ShouldBe(10);
        
        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey"
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();

        await _apiKeyService.UpdateApiKeyInfoCacheAsync(apiKeyInfo);

        var time = _clock.Now.AddMonths(-1);
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.LogEvent, "aaa.com", time);
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(0);
        
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(1);
        summary.LastQueryTime.ShouldBe(time);

        var apiSummaryMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, time));
        var query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        var apiSummaryDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryDailySnapshotGrainId(orgId, time));
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(1);
        apiKeyInfo.LastQueryTime.ShouldBe(time);
        
        var apiMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(apiKey.Id, time));
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        var apiDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyDailySnapshotGrainId(apiKey.Id, time));
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);

        var apiQueryBasicApiGrain =
            _clusterClient.GetGrain<IApiKeyQueryBasicApiGrain>(
                GrainIdHelper.GenerateApiKeyQueryBasicApiGrainId(apiKey.Id, BasicApi.LogEvent));
        var apiQueryBasicApiInfo = await apiQueryBasicApiGrain.GetAsync();
        apiQueryBasicApiInfo.Api.ShouldBe(BasicApi.LogEvent);
        apiQueryBasicApiInfo.ApiKeyId.ShouldBe(apiKey.Id);
        apiQueryBasicApiInfo.TotalQuery.ShouldBe(1);
        apiQueryBasicApiInfo.LastQueryTime.ShouldBe(time);

        var apiQueryBasicApiMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryBasicApiMonthlySnapshotGrainId(apiKey.Id, BasicApi.LogEvent, time));
        query = await apiQueryBasicApiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        var apiQueryBasicApiDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryBasicApiDailySnapshotGrainId(apiKey.Id, BasicApi.LogEvent, time));
        query = await apiQueryBasicApiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);

        for (int i = 0; i < 10; i++)
        {
            await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.LogEvent, "aaa.com", _clock.Now.AddMonths(-1));
        }
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(10);
        
        query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(10);
        
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiQueryBasicApiInfo = await apiQueryBasicApiGrain.GetAsync();
        apiQueryBasicApiInfo.TotalQuery.ShouldBe(10);
        
        query = await apiQueryBasicApiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiQueryBasicApiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.LogEvent, "aaa.com", _clock.Now.AddMonths(-1));
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(10);
        
        query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(10);
        
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        apiQueryBasicApiInfo = await apiQueryBasicApiGrain.GetAsync();
        apiQueryBasicApiInfo.TotalQuery.ShouldBe(10);
        
        query = await apiQueryBasicApiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        query = await apiQueryBasicApiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(10);
        
        time = _clock.Now.AddMinutes(-5);
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.LogEvent, "aaa.com", time);
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(11);
        summary.LastQueryTime.ShouldBe(time);

        apiSummaryMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, time));
        query = await apiSummaryMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiSummaryDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(
                GrainIdHelper.GenerateApiKeySummaryDailySnapshotGrainId(orgId, time));
        query = await apiSummaryDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiKeyInfo = await apiKeyGrain.GetAsync();
        apiKeyInfo.TotalQuery.ShouldBe(11);
        apiKeyInfo.LastQueryTime.ShouldBe(time);
        
        apiMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(apiKey.Id, time));
        query = await apiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeySnapshotGrain>(
                GrainIdHelper.GenerateApiKeyDailySnapshotGrainId(apiKey.Id, time));
        query = await apiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);

        apiQueryBasicApiGrain =
            _clusterClient.GetGrain<IApiKeyQueryBasicApiGrain>(
                GrainIdHelper.GenerateApiKeyQueryBasicApiGrainId(apiKey.Id, BasicApi.LogEvent));
        apiQueryBasicApiInfo = await apiQueryBasicApiGrain.GetAsync();
        apiQueryBasicApiInfo.Api.ShouldBe(BasicApi.LogEvent);
        apiQueryBasicApiInfo.ApiKeyId.ShouldBe(apiKey.Id);
        apiQueryBasicApiInfo.TotalQuery.ShouldBe(11);
        apiQueryBasicApiInfo.LastQueryTime.ShouldBe(time);

        apiQueryBasicApiMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryBasicApiMonthlySnapshotGrainId(apiKey.Id, BasicApi.LogEvent, time));
        query = await apiQueryBasicApiMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        apiQueryBasicApiDailyMonthlyGrain =
            _clusterClient.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(
                GrainIdHelper.GenerateApiKeyQueryBasicApiDailySnapshotGrainId(apiKey.Id, BasicApi.LogEvent, time));
        query = await apiQueryBasicApiDailyMonthlyGrain.GetQueryCountAsync();
        query.ShouldBe(1);
        
        await _apiKeyService.IncreaseQueryBasicApiCountAsync("App", BasicApi.LogEvent, "aaa.com", time);
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(11);
        
        await _apiKeyService.IncreaseQueryBasicApiCountAsync("app", BasicApi.LogEvent, "aaa.com", time);
        await _apiKeyTrafficProvider.FlushAsync();
        
        summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(11);
    }
    
    [Fact]
    public async Task IncreaseQueryApiCount_Verify_Test()
    {
        var orgId = Guid.NewGuid();
        await _apiKeyService.SetQueryLimitAsync(orgId, 0);

        var createInput = new CreateApiKeyInput
        {
            Name = "ApiKey",
            IsEnableSpendingLimit = true,
            SpendingLimitUsdt = AeFinderApplicationConsts.ApiKeyQueryPrice * 3
        };
        var apiKey = await _apiKeyService.CreateApiKeyAsync(orgId, createInput);

        await _apiKeyService.SetAuthorisedDomainsAsync(orgId, apiKey.Id, new SetAuthorisedDomainInput
        {
            Domains = { "aaa.com", "*.bbb.com" }
        });

        await _apiKeyService.SetAuthorisedApisAsync(orgId, apiKey.Id, new SetAuthorisedApiInput()
        {
            Apis = { { BasicApi.Block, true } }
        });
        
        var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        var apiKeyInfo = await apiKeyGrain.GetAsync();

        await _apiKeyService.UpdateApiKeyInfoCacheAsync(apiKeyInfo);
        
        var time = _clock.Now.AddMinutes(-5);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Block, "aaa.com", time));
        exception.Message.ShouldBe("Api key query times insufficient.");
        
        await _apiKeyService.SetQueryLimitAsync(orgId, 10);
        await _apiKeyService.UpdateApiKeySummaryLimitCacheAsync(orgId, 10);
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Transaction, "aaa.com", time));
        exception.Message.ShouldBe("Unauthorized api.");
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Block, "a.aaa.com", time));
        exception.Message.ShouldBe("Unauthorized domain.");

        await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Block, "aaa.com", time);
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Block, "bbb.com", time);
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Block, "b.bbb.com", time);

        await _apiKeyTrafficProvider.FlushAsync();
        
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(orgId));
        var summary = await apiKeySummaryGrain.GetApiKeySummaryInfoAsync();
        summary.TotalQuery.ShouldBe(3);

        await _apiKeyService.UpdateApiKeyUsedCacheAsync(apiKey.Id, time, 3);
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Block, "aaa.com", time));
        exception.Message.ShouldBe("Api key unavailable.");
        
        var createInput2 = new CreateApiKeyInput
        {
            Name = "ApiKey2",
            IsEnableSpendingLimit = true,
            SpendingLimitUsdt = AeFinderApplicationConsts.ApiKeyQueryPrice * 3
        };
        var apiKey2 = await _apiKeyService.CreateApiKeyAsync(orgId, createInput2);
        apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKey.Id);
        apiKeyInfo = await apiKeyGrain.GetAsync();
        await _apiKeyService.UpdateApiKeyInfoCacheAsync(apiKeyInfo);

        await _apiKeyService.UpdateApiKeySummaryUsedCacheAsync(orgId, time, 10);
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _apiKeyService.IncreaseQueryBasicApiCountAsync(apiKey.Key, BasicApi.Block, "aaa.com", time));
        exception.Message.ShouldBe("Api key query times insufficient.");
    }
}