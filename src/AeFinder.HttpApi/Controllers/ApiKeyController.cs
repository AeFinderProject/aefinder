using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.Grains;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Timing;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("ApiKey")]
[Route("api/api-keys")]
public class ApiKeyController : AeFinderController
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IApiKeySnapshotService _apiKeySnapshotService;
    private readonly IClock _clock;

    public ApiKeyController(IOrganizationAppService organizationAppService, IApiKeyService apiKeyService,
        IApiKeySnapshotService apiKeySnapshotService, IClock clock)
    {
        _organizationAppService = organizationAppService;
        _apiKeyService = apiKeyService;
        _apiKeySnapshotService = apiKeySnapshotService;
        _clock = clock;
    }

    [HttpGet]
    [Route("summary")]
    [Authorize]
    public async Task<ApiKeySummaryDto> GetApiKeySummaryAsync()
    {
        var orgId = await GetOrganizationIdAsync();
        var summary = await _apiKeyService.GetApiKeySummaryAsync(orgId);

        var monthDate = _clock.Now.ToMonthDate();
        var monthlySnapshot = await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(orgId, new GetSnapshotInput
        {
            Type = SnapshotType.Monthly,
            BeginTime = monthDate,
            EndTime = monthDate
        });
        if (monthlySnapshot.Items.Count > 0)
        {
            summary.Query = monthlySnapshot.Items.First().Query;
        }

        return summary;
    }
    
    [HttpGet]
    [Route("summary-snapshots")]
    [Authorize]
    public async Task<ListResultDto<ApiKeySummarySnapshotDto>> GetApiKeySummarySnapshotsAsync(GetSnapshotInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(orgId, input);
    }

    [HttpPost]
    [Authorize]
    public async Task<ApiKeyDto> CreateApiKeyAsync(CreateApiKeyInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeyService.CreateApiKeyAsync(orgId, input);
    }
    
    [HttpPut]
    [Route("{id}")]
    [Authorize]
    public async Task UpdateApiKeyAsync(Guid organizationId, Guid id, UpdateApiKeyInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _apiKeyService.UpdateApiKeyAsync(orgId, id, input);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<ApiKeyDto>> GetApiKeysAsync(GetApiKeyInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeyService.GetApiKeysAsync(orgId, input);
    }

    [HttpGet]
    [Route("{id}")]
    [Authorize]
    public async Task<ApiKeyDto> GetApiKeyAsync(Guid id)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeyService.GetApiKeyAsync(orgId, id);
    }

    [HttpPost]
    [Route("{id}/key")]
    [Authorize]
    public async Task<RegenerateKeyDto> RegenerateKeyAsync(Guid id)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeyService.RegenerateKeyAsync(orgId, id);
    }
    
    [HttpDelete]
    [Route("{id}")]
    [Authorize]
    public async Task DeleteApiKeyAsync(Guid id)
    {
        var orgId = await GetOrganizationIdAsync();
        await _apiKeyService.DeleteApiKeyAsync(orgId, id);
    }
    
    [HttpPut]
    [Route("{id}/aeindexer")]
    [Authorize]
    public async Task SetAuthorisedAeIndexersAsync(Guid id, SetAuthorisedAeIndexerInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _apiKeyService.SetAuthorisedAeIndexersAsync(orgId, id, input);
    }
    
    [HttpDelete]
    [Route("{id}/aeindexer")]
    [Authorize]
    public async Task DeleteAuthorisedAeIndexersAsync(Guid id, [FromBody]SetAuthorisedAeIndexerInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _apiKeyService.DeleteAuthorisedAeIndexersAsync(orgId, id, input);
    }
    
    [HttpPut]
    [Route("{id}/domain")]
    [Authorize]
    public async Task SetAuthorisedDomainsAsync(Guid id, SetAuthorisedDomainInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _apiKeyService.SetAuthorisedDomainsAsync(orgId, id, input);
    }
    
    [HttpDelete]
    [Route("{id}/domain")]
    [Authorize]
    public async Task DeleteAuthorisedDomainsAsync(Guid id, [FromBody]SetAuthorisedDomainInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _apiKeyService.DeleteAuthorisedDomainsAsync(orgId, id, input);
    }
    
    [HttpPut]
    [Route("{id}/api")]
    [Authorize]
    public async Task SetAuthorisedApisAsync(Guid id, SetAuthorisedApiInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _apiKeyService.SetAuthorisedApisAsync(orgId, id, input);
    }
    
    [HttpGet]
    [Route("{id}/aeindexers")]
    [Authorize]
    public async Task<PagedResultDto<ApiKeyQueryAeIndexerDto>> GetApiKeyQueryAeIndexersAsync(Guid id, GetApiKeyQueryAeIndexerInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeyService.GetApiKeyQueryAeIndexersAsync(orgId, id, input);
    }
    
    [HttpGet]
    [Route("{id}/apis")]
    [Authorize]
    public async Task<PagedResultDto<ApiKeyQueryApiDto>> GetApiKeyQueryApisAsync(Guid id, GetApiKeyQueryApiInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeyService.GetApiKeyQueryApisAsync(orgId, id, input);
    }
    
    [HttpGet]
    [Route("{id}/snapshots")]
    [Authorize]
    public async Task<ListResultDto<ApiKeySnapshotDto>> GetApiKeySnapshotsAsync(Guid id, GetSnapshotInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeySnapshotService.GetApiKeySnapshotsAsync(orgId, id, input);
    }
    
    [HttpGet]
    [Route("{id}/aeindexer-snapshots")]
    [Authorize]
    public async Task<ListResultDto<ApiKeyQueryAeIndexerSnapshotDto>> GetApiKeyQueryAeIndexerSnapshotsAsync(Guid id, GetQueryAeIndexerSnapshotInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeySnapshotService.GetApiKeyQueryAeIndexerSnapshotsAsync(orgId, id, input);
    }
    
    [HttpGet]
    [Route("{id}/api-snapshots")]
    [Authorize]
    public async Task<ListResultDto<ApiKeyQueryBasicApiSnapshotDto>> GetApiKeyQueryBasicApiSnapshotsAsync(Guid id, GetQueryBasicApiSnapshotInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _apiKeySnapshotService.GetApiKeyQueryBasicApiSnapshotsAsync(orgId, id, input);
    }
    
    [HttpPost]
    [Route("limit")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task AdjustQueryLimitAsync(AdjustQueryLimitInput input)
    {
        var orgId = input.OrganizationId;
        await _apiKeyService.AdjustQueryLimitAsync(orgId, input.Count);
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}