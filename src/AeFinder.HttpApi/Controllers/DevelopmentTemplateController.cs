using System.Threading.Tasks;
using AeFinder.DevelopmentTemplate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("DevelopmentTemplate")]
[Route("api/dev-template")]
public class DevelopmentTemplateController : AeFinderController
{
    private readonly IDevelopmentTemplateAppService _developmentTemplateAppService;

    public DevelopmentTemplateController(IDevelopmentTemplateAppService developmentTemplateAppService)
    {
        _developmentTemplateAppService = developmentTemplateAppService;
    }
    
    [HttpGet]
    [Authorize]
    public async Task<FileContentResult> GenerateProjectAsync(GenerateProjectDto input)
    {
        return await _developmentTemplateAppService.GenerateProjectAsync(input);
    }
}