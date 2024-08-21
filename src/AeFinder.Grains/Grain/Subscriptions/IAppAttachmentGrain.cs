namespace AeFinder.Grains.Grain.Subscriptions;

public interface IAppAttachmentGrain: IGrainWithStringKey
{
    Task AddAttachmentAsync(string appId, string version, string fileKey, string fileName, string s3Key);
}