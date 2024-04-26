using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Studio;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    [HttpPost("apply")]
    [Authorize]
    public Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput input)
    {
        return _studioService.ApplyAeFinderAppNameAsync(input);
    }

    [HttpPost("update")]
    [Authorize]
    public Task<AddOrUpdateAeFinderAppDto> AddOrUpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
    {
        return _studioService.UpdateAeFinderAppAsync(input);
    }

    [HttpGet("info")]
    [Authorize]
    public Task<AeFinderAppInfoDto> GetAeFinderAppInfo()
    {
        return _studioService.GetAeFinderAppAsync();
    }

    [HttpGet("applist")]
    [Authorize]
    public Task<List<AeFinderAppInfo>> GetAeFinderAppList()
    {
        return _studioService.GetAeFinderAppListAsync();
    }

    [HttpPost("submitsubscription")]
    [Authorize]
    // public Task<string> SubmitSubscriptionInfoAsync([FromForm] SubscriptionInfo input, [ModelBinder(BinderType = typeof(JsonModelBinder))] SubscriptionManifestDto subscriptionManifest)
    // {
    public Task<string> SubmitSubscriptionInfoAsync([ModelBinder(BinderType = typeof(JsonModelBinder))] SubscriptionManifestDto subscriptionManifest)
    {
        var input = new SubscriptionInfo();
        var filePath = "/app/AeFinder.TokenApp.dll";
        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        string fileName = Path.GetFileName(filePath);
        IFormFile formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);
        input.AppDll = formFile;
        return _studioService.SubmitSubscriptionInfoAsync(input, subscriptionManifest);
    }

    [HttpPost("updateapp")]
    [Authorize]
    public Task<UpdateAeFinderAppDto> UpdateAeFinderAppAsync([FromForm] UpdateAeFinderAppInput input)
    {
        return _studioService.UpdateAeFinderAppAsync(input);
    }
}