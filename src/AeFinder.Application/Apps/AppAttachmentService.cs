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

    public string GenerateAppJsonFileS3Key(string appId, string version, string fileName)
    {
        return appId + "/" + version + "-" + fileName;
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

            var s3Key = GenerateAppJsonFileS3Key(appId, version, fileNameWithExtension);
            await _awsS3ClientService.UpLoadJsonFileAsync(fileStream, s3Key);

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
        var s3Key = GenerateAppJsonFileS3Key(appId, version, fileName);
        await _awsS3ClientService.DeleteJsonFileAsync(s3Key);
        await appAttachmentGrain.RemoveAttachmentAsync(fileKey);
    }
}