using AeFinder.Subscriptions.Dto;

namespace AeFinder.Grains.Grain.Subscriptions;

public interface IAppAttachmentGrain: IGrainWithStringKey
{
    Task AddAttachmentAsync(string appId, string version, string fileKey, string fileName);
    Task RemoveAttachmentAsync(string fileKey);
    Task<string> GetAttachmentFileNameAsync(string fileKey);
    Task<List<AttachmentInfoDto>> GetAllAttachmentsInfoAsync();
    Task ClearGrainStateAsync();
}