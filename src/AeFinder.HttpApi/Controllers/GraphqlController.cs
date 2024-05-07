using System.Threading.Tasks;
using AeFinder.GraphQL;
using AeFinder.GraphQL.Dto;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Graphql")]
[Route("api/app/graphql")]
public class GraphqlController : AbpController
{
    private readonly IGraphQLAppService _graphQLAppService;

    public GraphqlController(IGraphQLAppService graphQLAppService)
    {
        _graphQLAppService = graphQLAppService;
    }

    [HttpPost("{appId}/{version?}")]
    public async Task<IActionResult> GraphqlForward([FromBody] GraphQLQueryInput input, string appId,
        string version = null)
    {
        var response = await _graphQLAppService.RequestForwardAsync(appId, version, input);

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