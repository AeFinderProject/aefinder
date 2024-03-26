using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Studio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Studio")]
[Route("api/app/studio")]
public class StudioController : AeFinderController
{
    private readonly IStudioService _studioService;

    public StudioController(IStudioService studioService) 
    {
        _studioService = studioService;
    }

    [HttpGet("apply")]
    [Authorize]
    public Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput input)
    {
        return _studioService.ApplyAeFinderAppName(input);
    }

    [HttpGet("update")]
    [Authorize]
    public Task<AddOrUpdateAeFinderAppDto> AddOrUpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
    {
        return _studioService.UpdateAeFinderApp(input);
    }

    // [HttpPost("adddeveloper")]
    // [Authorize]
    // public Task<AddDeveloperToAppDto> AddDeveloperToApp(AddDeveloperToAppInput input)
    // {
    //     // return _studioService.AddDeveloperToApp(input);
    // }

    [HttpGet("info")]
    [Authorize]
    public Task<AeFinderAppInfoDto> GetAeFinderAppInfo()
    {
        return _studioService.GetAeFinderApp();
    }

    [HttpGet("applist")]
    [Authorize]
    public Task<List<AeFinderAppInfo>> GetAeFinderAppList()
    {
        return _studioService.GetAeFinderAppList();
    }

    [HttpPost("submitsubscription")]
    [Authorize]
    public Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input)
    {
        var m = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = "AELF",
                    StartBlockNumber = 100,
                    OnlyConfirmed = true,
                    TransactionConditions = new List<TransactionConditionDto>()
                    {
                        new()
                        {
                            To = "23GxsoW9TRpLqX1Z5tjrmcRMMSn5bhtLAf4HtPj8JX9BerqTqp",
                            MethodNames = new List<string>()
                            {
                                "Transfer"
                            }
                        }
                    },
                }
            }
        };
        var json = JsonConvert.SerializeObject(m);
        return _studioService.SubmitSubscriptionInfoAsync(input);
    }

    // [HttpPost("query")]
    // [Authorize]
    // public Task<QueryAeFinderAppDto> QueryAeFinderApp(QueryAeFinderAppInput input)
    // {
    //     return _studioService.QueryAeFinderAppAsync(input);
    // }
    //
    // [HttpPost("logs")]
    // [Authorize]
    // public Task<QueryAeFinderAppLogsDto> QueryAeFinderAppLogs(QueryAeFinderAppLogsInput input)
    // {
    //     return _studioService.QueryAeFinderAppLogsAsync(input);
    // }
}