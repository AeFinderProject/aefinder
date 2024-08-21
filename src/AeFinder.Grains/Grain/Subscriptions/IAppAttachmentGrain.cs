using AeFinder.Subscriptions.Dto;

namespace AeFinder.Grains.Grain.Subscriptions;

public interface IAppAttachmentGrain: IGrainWithStringKey
{
    Task AddAttachmentAsync(string appId, string version, string fileKey, string fileName, string s3Key);
    Task RemoveAttachmentAsync(string fileKey);
    Task<string> GetAttachmentAwsS3keyAsync(string fileKey);
    Task<List<AttachmentInfoDto>> GetAllAttachmentsInfoAsync();
}