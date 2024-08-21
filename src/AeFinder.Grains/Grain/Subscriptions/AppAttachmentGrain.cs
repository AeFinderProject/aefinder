using AeFinder.Grains.State.Subscriptions;
using Microsoft.Extensions.Logging;

namespace AeFinder.Grains.Grain.Subscriptions;

public class AppAttachmentGrain: AeFinderGrain<AppAttachmentState>, IAppAttachmentGrain
{
    private readonly ILogger<AppAttachmentGrain> _logger;

    public AppAttachmentGrain(ILogger<AppAttachmentGrain> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
    }

    public async Task AddAttachmentAsync(string appId, string version, string fileKey, string fileName, string s3Key)
    {
        if (State.AttachmentInfos == null)
        {
            State.AttachmentInfos = new Dictionary<string, AttachmentInfo>();
        }

        var attachment = new AttachmentInfo()
        {
            AppId = appId,
            Version = version,
            FileKey = fileKey,
            FileName = fileName,
            AwsS3Key = s3Key
        };
        if (State.AttachmentInfos.Keys.Contains(fileKey))
        {
            State.AttachmentInfos[fileKey] = attachment;
        }
        else
        {
            State.AttachmentInfos.Add(fileKey, attachment);
        }

        await WriteStateAsync();
    }

    public async Task RemoveAttachmentAsync(string fileKey)
    {
        State.AttachmentInfos.Remove(fileKey);
        await WriteStateAsync();
    }

    public async Task<string> GetAttachmentAwsS3keyAsync(string fileKey)
    {
        if (State.AttachmentInfos.Keys.Contains(fileKey))
        {
            return State.AttachmentInfos[fileKey].AwsS3Key;
        }

        return string.Empty;
    }
}