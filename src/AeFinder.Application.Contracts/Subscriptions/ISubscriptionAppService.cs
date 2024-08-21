using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using Microsoft.AspNetCore.Http;

namespace AeFinder.Subscriptions;

public interface ISubscriptionAppService
{
    Task<string> AddSubscriptionAsync(string appId, SubscriptionManifestDto manifest, byte[] code,
        IFormFile attachment1, IFormFile attachment2, IFormFile attachment3, IFormFile attachment4,
        IFormFile attachment5);
    Task UpdateSubscriptionManifestAsync(string appId, string version, SubscriptionManifestDto manifest);
    Task UpdateSubscriptionCodeAsync(string appId, string version, byte[] code);
    Task UpdateSubscriptionAttachmentAsync(string appId, string version, List<string> attachmentDeleteFileKeyList,
        IFormFile attachment1, IFormFile attachment2, IFormFile attachment3, IFormFile attachment4,
        IFormFile attachment5);
    Task<AllSubscriptionDto> GetSubscriptionManifestAsync(string appId);
    Task<List<SubscriptionIndexDto>> GetSubscriptionManifestIndexAsync(string appId);
}