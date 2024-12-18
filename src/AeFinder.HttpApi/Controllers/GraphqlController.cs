using System.Threading.Tasks;
using AeFinder.ApiTraffic;
using AeFinder.GraphQL;
using AeFinder.GraphQL.Dto;
using AeFinder.Kubernetes;
using AeFinder.Options;
using AElf.OpenTelemetry.ExecutionTime;
using Asp.Versioning;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Graphql")]
[AggregateExecutionTime]
public class GraphqlController : AbpController
{
    private readonly IGraphQLAppService _graphQLAppService;
    private readonly KubernetesOptions _kubernetesOption;
    private readonly IApiTrafficService _apiTrafficService;

    public GraphqlController(IGraphQLAppService graphQLAppService,
        IOptionsSnapshot<KubernetesOptions> kubernetesOption, IApiTrafficService apiTrafficService)
    {
        _graphQLAppService = graphQLAppService;
        _apiTrafficService = apiTrafficService;
        _kubernetesOption = kubernetesOption.Value;
    }
    
    [EnableCors("AllowAnyCorsPolicy")]
    [HttpPost("api/{key}/graphql/{appId}/{version?}")]
    public virtual async Task<IActionResult> GraphqlForward([FromBody] GraphQLQueryInput input, string key, string appId,
        string version = null)
    {
        await _apiTrafficService.IncreaseRequestCountAsync(key);
        
        var response =
            await _graphQLAppService.RequestForwardAsync(appId, version, _kubernetesOption.OriginName, input);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, "application/json");
        }
        else
        {
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}