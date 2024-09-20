using Microsoft.AspNetCore.Mvc;

namespace AeFinder.RequestProxy;

[Route("api/request-proxy")]
[ApiController]
public class RequestProxyController : ControllerBase
{
    private readonly RequestProxyService _requestProxyService;

    public RequestProxyController(RequestProxyService requestProxyService)
    {
        _requestProxyService = requestProxyService;
    }
    
    [HttpGet("{path}/search")]
    public async Task<IActionResult> ForwardSearchRequest([FromRoute] string path, [FromBody] object requestPayload)
    {
        var result = await _requestProxyService.ForwardSearchGetRequestAsync(path, "_search", requestPayload.ToString());
        
        return Ok(result);
    }
    
    [HttpGet("{path}/count")]
    public async Task<IActionResult> ForwardCountRequest([FromRoute] string path, [FromBody] object requestPayload)
    {
        var result = await _requestProxyService.ForwardSearchGetRequestAsync(path, "_count", requestPayload.ToString());
        
        return Ok(result);
    }
}