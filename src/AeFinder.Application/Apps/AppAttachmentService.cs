using System;
using System.IO;
using System.Threading.Tasks;
using AeFinder.AmazonCloud;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.AspNetCore.Http;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
namespace AeFinder.Apps;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppAttachmentService : AeFinderAppService, IAppAttachmentService
{
    private readonly IClusterClient _clusterClient;
    private readonly IAwsS3ClientService _awsS3ClientService;

    public AppAttachmentService(IClusterClient clusterClient,
        IAwsS3ClientService awsS3ClientService)
    {
        _clusterClient = clusterClient;
        _awsS3ClientService = awsS3ClientService;
    }

    private string GenerateAppAwsS3FileName(string version, string fileName)
    {
        return version + "-" + fileName;
    }

    public async Task UploadAppAttachmentAsync(IFormFile file, string appId, string version)
    {
        using (Stream fileStream = file.OpenReadStream())
        {
            string fileNameWithExtension = file.FileName;
            string extension = Path.GetExtension(fileNameWithExtension);
            bool hasExtension = !string.IsNullOrEmpty(extension);
            if (!hasExtension)
            {
                throw new UserFriendlyException("Invalid file. only support json file.");
            }

            bool isJsonFile = extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
            if (isJsonFile)
            {
                throw new UserFriendlyException("Invalid file. only support json file.");
            }

            var s3FileName = GenerateAppAwsS3FileName(version, fileNameWithExtension);
            await _awsS3ClientService.UpLoadJsonFileAsync(fileStream, appId, s3FileName);

            string fileKey = Path.GetFileNameWithoutExtension(fileNameWithExtension); //Use file name as file key
            var appAttachmentGrain =
                _clusterClient.GetGrain<IAppAttachmentGrain>(
                    GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));
            await appAttachmentGrain.AddAttachmentAsync(appId, version, fileKey,
                fileNameWithExtension);
        }
    }

    public async Task DeleteAppAttachmentAsync(string appId, string version,string fileKey)
    {
        var appAttachmentGrain =
            _clusterClient.GetGrain<IAppAttachmentGrain>(
                GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));
        var fileName = await appAttachmentGrain.GetAttachmentFileNameAsync(fileKey);
        var s3FileName = GenerateAppAwsS3FileName(version, fileName);
        await _awsS3ClientService.DeleteJsonFileAsync(appId, s3FileName);
        await appAttachmentGrain.RemoveAttachmentAsync(fileKey);
    }
    
    public async Task DeleteAllAppAttachmentsAsync(string appId, string version)
    {
        var appAttachmentGrain = _clusterClient.GetGrain<IAppAttachmentGrain>(GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));

        var attachmentInfos = await appAttachmentGrain.GetAllAttachmentsInfoAsync();
        
        foreach (var attachmentInfo in attachmentInfos)
        {
            var s3FileName = GenerateAppAwsS3FileName(version, attachmentInfo.FileName);
            await _awsS3ClientService.DeleteJsonFileAsync(appId, s3FileName);
        }

        await appAttachmentGrain.RemoveAllAttachmentsAsync();
    }

    public async Task<string> GetAppAttachmentContentAsync(string appId, string version, string fileKey)
    {
        var appAttachmentGrain = _clusterClient.GetGrain<IAppAttachmentGrain>(
            GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));
        var fileName = await appAttachmentGrain.GetAttachmentFileNameAsync(fileKey);
        if (fileName.IsNullOrWhiteSpace())
        {
            throw new BusinessException("No attachment for key： {Key}", fileKey);
        }

        return await _awsS3ClientService.GetJsonFileContentAsync(appId, GenerateAppAwsS3FileName(version, fileName));
    }
}