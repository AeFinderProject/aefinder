using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Models;
using AeFinder.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Subscription")]
[Route("api/apps/subscriptions")]
public class SubscriptionController : AeFinderController
{
    private readonly ISubscriptionAppService _subscriptionAppService;

    public SubscriptionController(ISubscriptionAppService subscriptionAppService)
    {
        _subscriptionAppService = subscriptionAppService;
    }

    [HttpPost]
    [Authorize]
    public async Task<string> AddSubscriptionAsync([FromForm]AddSubscriptionInput input)
    {
        CheckFile(input.Code);
        return await _subscriptionAppService.AddSubscriptionAsync(ClientId, input.Manifest,input.Code.GetAllBytes());
    }
    
    [HttpPut]
    [Authorize]
    [Route("manifest/{version}")]
    public async Task UpdateManifestAsync(string version, SubscriptionManifestDto input)
    {
        await _subscriptionAppService.UpdateSubscriptionManifestAsync(ClientId, version, input);
    }
    
    [HttpPut]
    [Authorize]
    [Route("code/{version}")]
    public async Task UpdateCodeAsync(string version, [FromForm]UpdateSubscriptionCodeInput input)
    {
        CheckFile(input.Code);
        await _subscriptionAppService.UpdateSubscriptionCodeAsync(ClientId, version, input.Code.GetAllBytes());
    }
    
    [HttpGet]
    [Authorize]
    public async Task<AllSubscriptionDto> GetSubscriptionAsync()
    {
        return await _subscriptionAppService.GetSubscriptionManifestAsync(ClientId);
    }

    private void CheckFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new UserFriendlyException("File is empty.");
        }
    }
}