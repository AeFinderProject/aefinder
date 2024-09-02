using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AeFinder.AmazonCloud;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly AppDeployOptions _appDeployOptions;

    public AppAttachmentService(IClusterClient clusterClient, IOptionsSnapshot<AppDeployOptions> appDeployOptions,
        IAwsS3ClientService awsS3ClientService)
    {
        _clusterClient = clusterClient;
        _awsS3ClientService = awsS3ClientService;
        _appDeployOptions = appDeployOptions.Value;
    }

    private string GenerateAppAwsS3FileName(string version, string fileName)
    {
        return version + "-" + fileName;
    }

    public async Task UploadAppAttachmentListAsync(List<IFormFile> attachmentList, string appId, string version)
    {
        await CheckAttachmentListAsync(attachmentList, appId, version);
        
        long totalFileSize = 0L;
        var appAttachmentGrain =
            _clusterClient.GetGrain<IAppAttachmentGrain>(
                GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));
        var oldAttachmentFileSize = await appAttachmentGrain.GetAllAttachmentsFileSizeAsync();
        totalFileSize = totalFileSize + oldAttachmentFileSize;
        
        foreach (var attachment in attachmentList)
        {
            if (attachment == null)
            {
                continue;
            }
                
            var fileSize = attachment.Length;
            string fileNameWithExtension = attachment.FileName;
            string extension = Path.GetExtension(fileNameWithExtension);
            Logger.LogInformation("Attachment file name: {0} extension: {1} size: {2}", fileNameWithExtension, extension,
                fileSize);
            bool hasExtension = !string.IsNullOrEmpty(extension);
            if (!hasExtension)
            {
                throw new UserFriendlyException("Invalid file.");
            }
            
            bool isFileValid = fileNameWithExtension.Contains(".json", StringComparison.OrdinalIgnoreCase);
            if (!isFileValid)
            {
                throw new UserFriendlyException("Invalid file. only support json file.");
            }
                
            string fileKey = Path.GetFileNameWithoutExtension(fileNameWithExtension); //Use file name as file key
            bool isZipFile = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
            if (isZipFile)
            {
                fileKey = fileKey.Replace(".json", "");
            }
                
            if (!Regex.IsMatch(fileKey, @"^[a-zA-Z0-9_-]+$"))
            {
                throw new UserFriendlyException("File name can only contain letters, numbers, underscores, hyphens.");
            }

            if (isZipFile)
            {
                fileNameWithExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);
                var compressedData = ZipHelper.ConvertIFormFileToByteArray(attachment);
                string jsonData = ZipHelper.DecompressDeflateData(compressedData);
                if (!IsValidJson(jsonData))
                {
                    throw new UserFriendlyException($"Attachment {fileNameWithExtension} json is not valid.");
                }
                var fileStream = ZipHelper.ConvertStringToStream(jsonData);
                fileSize = fileStream.Length;
                totalFileSize = totalFileSize + fileSize;
                Logger.LogInformation("Decompress file name: {0} extension: {1} size: {2} ,totalSize: {3}",
                    fileNameWithExtension, extension,
                    fileSize, totalFileSize);
                if (totalFileSize > _appDeployOptions.MaxAppAttachmentSize)
                {
                    throw new UserFriendlyException("Attachment's total size is too Large.");
                }
                await UploadAppAttachmentAsync(fileStream, appId, version, fileKey, fileNameWithExtension,
                    fileSize);
                continue;
            }
            
            bool isJsonFile = extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
            if (isJsonFile)
            {
                totalFileSize = totalFileSize + fileSize;
                if (totalFileSize > _appDeployOptions.MaxAppAttachmentSize)
                {
                    throw new UserFriendlyException("Attachment's total size is too Large.");
                }
                using (Stream fileStream = attachment.OpenReadStream())
                {
                    await UploadAppAttachmentAsync(fileStream, appId, version, fileKey, fileNameWithExtension,
                        fileSize);
                }
            }
            else
            {
                throw new UserFriendlyException("Invalid file type. only support json file.");
            }
            
        }
        
    }

    private async Task CheckAttachmentListAsync(List<IFormFile> attachmentList, string appId, string version)
    {
        if (attachmentList == null)
        {
            Logger.LogWarning("Attachment list is null.");
            return;
        }
        var appAttachmentGrain =
            _clusterClient.GetGrain<IAppAttachmentGrain>(GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));
        var attachmentInfos = await appAttachmentGrain.GetAllAttachmentsInfoAsync();
        var fileCount = attachmentList.Count;
        fileCount = fileCount + attachmentInfos.Count;
        if (fileCount > 5)
        {
            throw new UserFriendlyException("Only support 5 attachments.");
        }
    }
    
    public static bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException exception) 
        {
            return false;
        }
    }

    public async Task UploadAppAttachmentAsync(Stream fileStream, string appId, string version, string fileKey,
        string fileNameWithExtension, long fileSize)
    {
        var s3FileName = GenerateAppAwsS3FileName(version, fileNameWithExtension);
        await _awsS3ClientService.UpLoadJsonFileAsync(fileStream, appId, s3FileName);

        var appAttachmentGrain =
            _clusterClient.GetGrain<IAppAttachmentGrain>(
                GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));
        await appAttachmentGrain.AddAttachmentAsync(appId, version, fileKey,
            fileNameWithExtension, fileSize);
        Logger.LogInformation($"UpLoad Json File {s3FileName} successfully");
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

        await appAttachmentGrain.ClearGrainStateAsync();
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