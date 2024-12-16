using System;
using System.Threading.Tasks;
using AeFinder.Grains;
using Nest;
using Shouldly;
using Volo.Abp.Timing;
using Xunit;

namespace AeFinder.ApiKeys;

public class ApiKeySnapshotServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IApiKeySnapshotService _apiKeySnapshotService;
    private readonly IClock _clock;

    public ApiKeySnapshotServiceTests()
    {
        _apiKeySnapshotService = GetRequiredService<IApiKeySnapshotService>();
        _clock = GetRequiredService<IClock>();
    }

    [Fact]
    public async Task GetApiKeySummarySnapshots_Test()
    {
        var orgId = Guid.NewGuid();
        var input1 = new ApiKeySummarySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, _clock.Now.AddMonths(-1)),
            Query = 10,
            Time = _clock.Now.AddMonths(-1).ToMonthDate(),
            Type = SnapshotType.Monthly
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySummarySnapshotIndexAsync(input1);
        
        var input2 = new ApiKeySummarySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(orgId, _clock.Now),
            Query = 20,
            Time = _clock.Now.ToMonthDate(),
            Type = SnapshotType.Monthly
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySummarySnapshotIndexAsync(input2);
        
        var input3 = new ApiKeySummarySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeySummaryDailySnapshotGrainId(orgId, _clock.Now),
            Query = 1,
            Time = _clock.Now.Date,
            Type = SnapshotType.Daily
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySummarySnapshotIndexAsync(input3);
        
        var snapshots = await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(orgId,new GetSnapshotInput
        {
            Type = SnapshotType.Daily
        });
        snapshots.Items.Count.ShouldBe(1);
        
        snapshots = await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(orgId,new GetSnapshotInput
        {
            Type = SnapshotType.Monthly
        });
        snapshots.Items.Count.ShouldBe(2);

        snapshots = await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(orgId,new GetSnapshotInput
        {
            Type = SnapshotType.Monthly,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate()
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);
        
        snapshots = await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(orgId,new GetSnapshotInput
        {
            Type = SnapshotType.Daily,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate().AddMonths(1)
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(1);
    }
    
    [Fact]
    public async Task GetApiKeySnapshots_Test()
    {
        var orgId = Guid.NewGuid();
        var apiKeyId1 = Guid.NewGuid();
        var apiKeyId2 = Guid.NewGuid();
        var input1 = new ApiKeySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(orgId, _clock.Now.AddMonths(-1)),
            Query = 10,
            Time = _clock.Now.AddMonths(-1).ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = apiKeyId1
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySnapshotIndexAsync(input1);
        
        var input2 = new ApiKeySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(orgId, _clock.Now),
            Query = 20,
            Time = _clock.Now.ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = apiKeyId2,
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySnapshotIndexAsync(input2);
        
        var input3 = new ApiKeySnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyDailySnapshotGrainId(orgId, _clock.Now),
            Query = 1,
            Time = _clock.Now.Date,
            Type = SnapshotType.Daily,
            ApiKeyId = apiKeyId2
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeySnapshotIndexAsync(input3);

        var snapshots = await _apiKeySnapshotService.GetApiKeySnapshotsAsync(orgId, null, new GetSnapshotInput
        {
            Type = SnapshotType.Daily
        });
        snapshots.Items.Count.ShouldBe(1);
        
        snapshots = await _apiKeySnapshotService.GetApiKeySnapshotsAsync(orgId, null, new GetSnapshotInput
        {
            Type = SnapshotType.Monthly
        });
        snapshots.Items.Count.ShouldBe(2);

        snapshots = await _apiKeySnapshotService.GetApiKeySnapshotsAsync(orgId, null, new GetSnapshotInput
        {
            Type = SnapshotType.Monthly,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate()
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);
        
        snapshots = await _apiKeySnapshotService.GetApiKeySnapshotsAsync(orgId, null, new GetSnapshotInput
        {
            Type = SnapshotType.Daily,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate().AddMonths(1)
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(1);
        
        snapshots = await _apiKeySnapshotService.GetApiKeySnapshotsAsync(orgId, apiKeyId2, new GetSnapshotInput
        {
            Type = SnapshotType.Monthly
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);
    }
    
    [Fact]
    public async Task GetApiKeyQueryAeIndexerSnapshots_Test()
    {
        var orgId = Guid.NewGuid();
        var apiKeyId1 = Guid.NewGuid();
        var apiKeyId2 = Guid.NewGuid();
        var input1 = new ApiKeyQueryAeIndexerSnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(orgId, "app1", _clock.Now.AddMonths(-1)),
            Query = 10,
            Time = _clock.Now.AddMonths(-1).ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = apiKeyId1,
            AppId = "app1",
            AppName = "appName1"
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(input1);
        
        var input2 = new ApiKeyQueryAeIndexerSnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(orgId, "app1", _clock.Now),
            Query = 20,
            Time = _clock.Now.ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = apiKeyId2,
            AppId = "app1",
            AppName = "appName1"
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(input2);
        
        var input3 = new ApiKeyQueryAeIndexerSnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(orgId, "app2", _clock.Now),
            Query = 1,
            Time = _clock.Now.Date,
            Type = SnapshotType.Daily,
            ApiKeyId = apiKeyId2,
            AppId = "app2",
            AppName = "appName2"
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(input3);

        var snapshots = await _apiKeySnapshotService.GetApiKeyQueryAeIndexerSnapshotsAsync(orgId, apiKeyId1, new GetQueryAeIndexerSnapshotInput
        {
            Type = SnapshotType.Monthly
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(10);
        
        snapshots = await _apiKeySnapshotService.GetApiKeyQueryAeIndexerSnapshotsAsync(orgId, apiKeyId2, new GetQueryAeIndexerSnapshotInput
        {
            Type = SnapshotType.Monthly
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);

        snapshots = await _apiKeySnapshotService.GetApiKeyQueryAeIndexerSnapshotsAsync(orgId, apiKeyId2, new GetQueryAeIndexerSnapshotInput
        {
            Type = SnapshotType.Monthly,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate()
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);
        
        snapshots = await _apiKeySnapshotService.GetApiKeyQueryAeIndexerSnapshotsAsync(orgId, apiKeyId2, new GetQueryAeIndexerSnapshotInput
        {
            Type = SnapshotType.Daily,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate().AddMonths(1)
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(1);
        
        snapshots = await _apiKeySnapshotService.GetApiKeyQueryAeIndexerSnapshotsAsync(orgId, apiKeyId2, new GetQueryAeIndexerSnapshotInput
        {
            Type = SnapshotType.Monthly,
            AppId = "app1"
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);
    }
    
    [Fact]
    public async Task GetApiKeyQueryApiSnapshots_Test()
    {
        var orgId = Guid.NewGuid();
        var apiKeyId1 = Guid.NewGuid();
        var apiKeyId2 = Guid.NewGuid();
        var input1 = new ApiKeyQueryBasicApiSnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyQueryBasicApiMonthlySnapshotGrainId(orgId, BasicApi.Block, _clock.Now.AddMonths(-1)),
            Query = 10,
            Time = _clock.Now.AddMonths(-1).ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = apiKeyId1,
            Api = BasicApi.Block
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(input1);
        
        var input2 = new ApiKeyQueryBasicApiSnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyQueryBasicApiMonthlySnapshotGrainId(orgId, BasicApi.Transaction, _clock.Now),
            Query = 20,
            Time = _clock.Now.ToMonthDate(),
            Type = SnapshotType.Monthly,
            ApiKeyId = apiKeyId2,
            Api = BasicApi.Transaction
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(input2);
        
        var input3 = new ApiKeyQueryBasicApiSnapshotChangedEto
        {
            OrganizationId = orgId,
            Id = GrainIdHelper.GenerateApiKeyQueryBasicApiMonthlySnapshotGrainId(orgId, BasicApi.LogEvent, _clock.Now),
            Query = 1,
            Time = _clock.Now.Date,
            Type = SnapshotType.Daily,
            ApiKeyId = apiKeyId2,
            Api = BasicApi.LogEvent
        };
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(input3);

        var snapshots = await _apiKeySnapshotService.GetApiKeyQueryBasicApiSnapshotsAsync(orgId, apiKeyId1, new GetQueryBasicApiSnapshotInput
        {
            Type = SnapshotType.Monthly
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(10);
        
        snapshots = await _apiKeySnapshotService.GetApiKeyQueryBasicApiSnapshotsAsync(orgId, apiKeyId2, new GetQueryBasicApiSnapshotInput
        {
            Type = SnapshotType.Monthly
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);

        snapshots = await _apiKeySnapshotService.GetApiKeyQueryBasicApiSnapshotsAsync(orgId, apiKeyId2, new GetQueryBasicApiSnapshotInput
        {
            Type = SnapshotType.Monthly,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate()
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(20);
        
        snapshots = await _apiKeySnapshotService.GetApiKeyQueryBasicApiSnapshotsAsync(orgId, apiKeyId2, new GetQueryBasicApiSnapshotInput
        {
            Type = SnapshotType.Daily,
            BeginTime = _clock.Now.ToMonthDate(),
            EndTime = _clock.Now.ToMonthDate().AddMonths(1)
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(1);
        
        snapshots = await _apiKeySnapshotService.GetApiKeyQueryBasicApiSnapshotsAsync(orgId, apiKeyId2, new GetQueryBasicApiSnapshotInput
        {
            Type = SnapshotType.Daily,
            Api = BasicApi.LogEvent
        });
        snapshots.Items.Count.ShouldBe(1);
        snapshots.Items[0].Query.ShouldBe(1);
    }
}