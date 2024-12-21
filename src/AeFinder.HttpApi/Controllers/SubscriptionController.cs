using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Models;
using AeFinder.Subscriptions;
using AeFinder.Subscriptions.Dto;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Authorization;

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
    [RequestSizeLimit(209715200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
    public async Task<string> AddSubscriptionAsync([FromForm]AddSubscriptionInput input)
    {
        await _subscriptionAppService.CheckPodResourceAsync(ClientId);
        CheckFile(input.Code);
        return await _subscriptionAppService.AddSubscriptionAsync(ClientId, input.Manifest, input.Code.GetAllBytes(),
            input.AttachmentList);
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
    [RequestSizeLimit(209715200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
    public async Task UpdateCodeAsync(string version, [FromForm]UpdateSubscriptionCodeInput input)
    {
        // CheckFile(input.Code);
        byte[] codeBytes = null;
        if (input.Code != null && input.Code.Length > 0)
        {
            codeBytes = input.Code.GetAllBytes();
        }

        await _subscriptionAppService.UpdateSubscriptionCodeAsync(ClientId, version, codeBytes,
            input.AttachmentDeleteFileKeyList, input.AttachmentList);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<AllSubscriptionDto> GetSubscriptionAsync()
    {
        return await _subscriptionAppService.GetSubscriptionManifestAsync(ClientId);
    }
    
    [HttpGet]
    [Authorize(Policy = "OnlyAdminAccess")]
    [Route("manifest/{appId}")]
    public async Task<List<SubscriptionIndexDto>> GetSubscriptionManifestIndexAsync(string appId)
    {
        return await _subscriptionAppService.GetSubscriptionManifestIndexAsync(appId);
    }

    private void CheckFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new UserFriendlyException("File is empty.");
        }
    }

    [HttpGet]
    [Authorize]
    [Route("attachments/{version}")]
    public async Task<List<AttachmentInfoDto>> GetSubscriptionAttachmentsAsync(string version)
    {
        return await _subscriptionAppService.GetSubscriptionAttachmentsAsync(ClientId, version);
    }
}