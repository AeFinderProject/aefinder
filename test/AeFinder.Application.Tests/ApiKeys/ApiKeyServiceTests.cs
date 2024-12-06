using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using Orleans;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Timing;
using Xunit;

namespace AeFinder.ApiKeys;

public class ApiKeyServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IApiKeySnapshotService _apiKeySnapshotService;
    private readonly IClusterClient _clusterClient;
    private readonly IClock _clock;

    public ApiKeyServiceTests()
    {
        _apiKeyService = GetRequiredService<IApiKeyService>();
        _apiKeySnapshotService = GetRequiredService<IApiKeySnapshotService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _clock = GetRequiredService<IClock>();
    }

    [Fact]
    public async Task Create_Tests()
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
        

        // var apiKeyDto = await _apiKeyService.GetApiKeyAsync(orgId, apiKey.Id);
        // apiKeyDto.Id.ShouldBe(apiKey.Id);
        // apiKeyDto.OrganizationId.ShouldBe(orgId);
        // apiKeyDto.Key.ShouldBe(apiKey.Key);
        // apiKeyDto.Name.ShouldBe(createInput.Name);
        // apiKeyDto.IsEnableSpendingLimit.ShouldBe(createInput.IsEnableSpendingLimit);
        // apiKeyDto.SpendingLimitUsdt.ShouldBe(createInput.SpendingLimitUsdt);
        // apiKeyDto.IsActive.ShouldBeTrue();
        // apiKeyDto.AuthorisedApis.Count.ShouldBe(0);
        // apiKeyDto.AuthorisedDomains.Count.ShouldBe(0);
        // apiKeyDto.AuthorisedAeIndexers.Count.ShouldBe(0);
        // apiKeyDto.TotalQuery.ShouldBe(0);
        // apiKeyDto.PeriodQuery.ShouldBe(0);
        // apiKeyDto.IsDeleted.ShouldBeFalse();
        //
        // var apiKeyList = await _apiKeyService.GetApiKeysAsync(orgId, new GetApiKeyInput());
        // apiKeyList.TotalCount.ShouldBe(1);
        // apiKeyList.Items.Count.ShouldBe(1);
        // apiKeyList.Items[0].Id.ShouldBe(apiKey.Id);
        // apiKeyList.Items[0].OrganizationId.ShouldBe(orgId);
        // apiKeyList.Items[0].Key.ShouldBe(apiKey.Key);
        // apiKeyList.Items[0].Name.ShouldBe(createInput.Name);
        // apiKeyList.Items[0].IsEnableSpendingLimit.ShouldBe(createInput.IsEnableSpendingLimit);
        // apiKeyList.Items[0].SpendingLimitUsdt.ShouldBe(createInput.SpendingLimitUsdt);
        // apiKeyList.Items[0].IsActive.ShouldBeTrue();
        // apiKeyList.Items[0].AuthorisedApis.Count.ShouldBe(0);
        // apiKeyList.Items[0].AuthorisedDomains.Count.ShouldBe(0);
        // apiKeyList.Items[0].AuthorisedAeIndexers.Count.ShouldBe(0);
        // apiKeyList.Items[0].TotalQuery.ShouldBe(0);
        // apiKeyList.Items[0].PeriodQuery.ShouldBe(0);
        // apiKeyList.Items[0].IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Update_Tests()
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
    }

    [Fact]
    public async Task RegenerateKey_Tests()
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
    public async Task Delete_Tests()
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
    public async Task SetAuthorisedAeIndexers_Tests()
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
    public async Task SetAuthorisedDomains_Tests()
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
    public async Task SetAuthorisedApisAsync_Tests()
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
    }

    [Fact]
    public async Task GetApiKey_Test()
    {
    }
}