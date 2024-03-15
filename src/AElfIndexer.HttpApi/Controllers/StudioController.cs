using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using AElfIndexer.Studio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AElfIndexer.Controllers;

[RemoteService]
[ControllerName("Studio")]
[Route("api/app/studio")]
public class StudioController : AElfIndexerController
{
    private readonly IStudioService _studioService;

    public StudioController(IStudioService studioService)
    {
        _studioService = studioService;
    }

    [HttpGet("apply")]
    [Authorize]
    public virtual Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(string name)
    {
        return _studioService.ApplyAeFinderAppName(ClientId, name);
    }

    [HttpPost("update")]
    [Authorize]
    public virtual Task<AddOrUpdateAeFinderAppDto> AddOrUpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
    {
        return _studioService.UpdateAeFinderApp(ClientId, input);
    }

    [HttpPost("info")]
    [Authorize]
    public virtual Task<AeFinderAppInfoDto> GetAeFinderAppInfo(GetAeFinderAppInfoInput input)
    {
        return _studioService.GetAeFinderApp(ClientId, input);
    }
}