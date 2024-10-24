using System.Threading.Tasks;
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
[Route("api/app/graphql")]
[AggregateExecutionTime]
public class GraphqlController : AbpController
{
    private readonly IGraphQLAppService _graphQLAppService;
    private readonly KubernetesOptions _kubernetesOption;

    public GraphqlController(IGraphQLAppService graphQLAppService,
        IOptionsSnapshot<KubernetesOptions> kubernetesOption)
    {
        _graphQLAppService = graphQLAppService;
        _kubernetesOption = kubernetesOption.Value;
    }
    
    [EnableCors("AllowAnyCorsPolicy")]
    [HttpPost("{appId}/{version?}")]
    public virtual async Task<IActionResult> GraphqlForward([FromBody] GraphQLQueryInput input, string appId,
        string version = null)
    {
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