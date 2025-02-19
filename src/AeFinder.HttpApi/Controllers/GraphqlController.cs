using System.Threading.Tasks;
using AeFinder.ApiKeys;
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
using Volo.Abp.Timing;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Graphql")]
[AggregateExecutionTime]
public class GraphqlController : AeFinderController
{
    private readonly IGraphQLAppService _graphQLAppService;
    private readonly KubernetesOptions _kubernetesOption;
    private readonly IApiKeyService _apiKeyService;
    private readonly IClock _clock;

    public GraphqlController(IGraphQLAppService graphQLAppService,
        IOptionsSnapshot<KubernetesOptions> kubernetesOption, IApiKeyService apiKeyService, IClock clock)
    {
        _graphQLAppService = graphQLAppService;
        _apiKeyService = apiKeyService;
        _clock = clock;
        _kubernetesOption = kubernetesOption.Value;
    }
    
    [EnableCors("AllowAnyCorsPolicy")]
    [HttpPost("api/{key}/graphql/{appId}/{version?}")]
    public virtual async Task<IActionResult> GraphqlForward([FromBody] GraphQLQueryInput input, string key, string appId,
        string version = null)
    {
        var response =
            await _graphQLAppService.RequestForwardAsync(appId, version, _kubernetesOption.OriginName, _kubernetesOption.AppNameSpace, input);

        if (response.IsSuccessStatusCode)
        {
            await _apiKeyService.IncreaseQueryAeIndexerCountAsync(key, appId, GetOriginHost(), _clock.Now);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, "application/json");
        }
        else
        {
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}