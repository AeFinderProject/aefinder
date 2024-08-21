using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Subscriptions.Dto;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Subscriptions;

public interface ISubscriptionAppService
{
    Task<string> AddSubscriptionAsync(string appId, SubscriptionManifestDto manifest, byte[] code,
        IFormFile attachment1, IFormFile attachment2, IFormFile attachment3, IFormFile attachment4,
        IFormFile attachment5, AddAttachmentInput attachments);
    Task UpdateSubscriptionManifestAsync(string appId, string version, SubscriptionManifestDto manifest);
    Task UpdateSubscriptionCodeAsync(string appId, string version, byte[] code);
    Task<AllSubscriptionDto> GetSubscriptionManifestAsync(string appId);
    Task<List<SubscriptionIndexDto>> GetSubscriptionManifestIndexAsync(string appId);
}