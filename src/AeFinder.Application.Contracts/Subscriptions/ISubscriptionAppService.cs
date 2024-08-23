using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Subscriptions.Dto;
using Microsoft.AspNetCore.Http;

namespace AeFinder.Subscriptions;

public interface ISubscriptionAppService
{
    Task<string> AddSubscriptionAsync(string appId, SubscriptionManifestDto manifest, byte[] code,
        List<IFormFile> attachmentList);
    Task UpdateSubscriptionManifestAsync(string appId, string version, SubscriptionManifestDto manifest);

    Task UpdateSubscriptionCodeAsync(string appId, string version, byte[] code, string attachmentDeleteFileKeyList,
        List<IFormFile> attachmentList);
    Task UpdateSubscriptionAttachmentAsync(string appId, string version, string attachmentDeleteFileKeyList,
        List<IFormFile> attachmentList);
    Task<AllSubscriptionDto> GetSubscriptionManifestAsync(string appId);
    Task<List<SubscriptionIndexDto>> GetSubscriptionManifestIndexAsync(string appId);
    Task<List<AttachmentInfoDto>> GetSubscriptionAttachmentsAsync(string appId, string version);
}