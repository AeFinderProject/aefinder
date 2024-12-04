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

    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}