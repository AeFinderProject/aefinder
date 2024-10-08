using AeFinder.Grains.State.Subscriptions;
using AeFinder.Subscriptions.Dto;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Subscriptions;

public class AppAttachmentGrain: AeFinderGrain<AppAttachmentState>, IAppAttachmentGrain
{
    private readonly IObjectMapper _objectMapper;

    public AppAttachmentGrain(ILogger<AppAttachmentGrain> logger, IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
    }

    public async Task AddAttachmentAsync(string appId, string version, string fileKey, string fileName, long fileSize)
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
            FileSize = fileSize
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

    public Task<string> GetAttachmentFileNameAsync(string fileKey)
    {
        if (State.AttachmentInfos.Keys.Contains(fileKey))
        {
            return Task.FromResult(State.AttachmentInfos[fileKey].FileName);
        }

        return Task.FromResult(string.Empty);
    }

    public async Task<List<AttachmentInfoDto>> GetAllAttachmentsInfoAsync()
    {
        var resultList = new List<AttachmentInfoDto>();
        if (State.AttachmentInfos != null && State.AttachmentInfos.Count > 0)
        {
            foreach (var attachmentInfoKeyValuePair in State.AttachmentInfos)
            {
                var attachInfo = attachmentInfoKeyValuePair.Value;
                var attachInfoDto = _objectMapper.Map<AttachmentInfo, AttachmentInfoDto>(attachInfo);
                resultList.Add(attachInfoDto);
            }
        }

        return resultList;
    }

    public async Task<long> GetAllAttachmentsFileSizeAsync()
    {
        long attachmentsFileSize = 0;
        if (State.AttachmentInfos != null && State.AttachmentInfos.Count > 0)
        {
            foreach (var attachmentInfoKeyValuePair in State.AttachmentInfos)
            {
                var attachInfo = attachmentInfoKeyValuePair.Value;
                attachmentsFileSize = attachmentsFileSize + attachInfo.FileSize;
            }
        }

        return attachmentsFileSize;
    }

    public async Task ClearGrainStateAsync()
    {
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}