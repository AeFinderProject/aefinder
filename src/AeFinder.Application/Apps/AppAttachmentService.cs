using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AeFinder.AmazonCloud;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

    private async Task UploadAppZipAttachmentAsync(IFormFile file, string appId, string version)
    {
        string fileNameWithExtension = file.FileName;
        string extension = Path.GetExtension(fileNameWithExtension);
        Logger.LogInformation("Attachment file name: {0} extension: {1}", fileNameWithExtension, extension);
        string fileNameWithJsonExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension); //Use file name as file key
        string fileKey = fileNameWithJsonExtension.Replace(".json", "");
        
        var compressedData = await ConvertIFormFileToByteArray(file);
        // string jsonData = DecompressPakoZip(compressedData);
        string jsonData = ZipHelper.DecompressDeflateData(compressedData);
        // Logger.LogInformation("File json data: "+jsonData);
        // string tempFileName = Path.GetTempFileName();
        var s3FileName = GenerateAppAwsS3FileName(version, fileNameWithJsonExtension);
        
        await File.WriteAllTextAsync(s3FileName, jsonData);
        var fileStream = await ConvertStringToStream(jsonData);
        await _awsS3ClientService.UpLoadJsonFileAsync(fileStream, appId, s3FileName);
        File.Delete(s3FileName);
        Logger.LogInformation($"UpLoad Json File {s3FileName} successfully");
    }
    
    private async Task<byte[]> ConvertIFormFileToByteArray(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
    
    private async Task<Stream> ConvertStringToStream(string jsonData)
    {
        var memoryStream = new MemoryStream();

        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
        {
            await streamWriter.WriteAsync(jsonData);
            await streamWriter.FlushAsync(); 
        }
        memoryStream.Position = 0;

        return memoryStream;
    }
    
    public async Task UploadAppAttachmentAsync(IFormFile file, string appId, string version)
    {
        var fileSize = file.Length;
        string fileNameWithExtension = file.FileName;
        string extension = Path.GetExtension(fileNameWithExtension);
        Logger.LogInformation("Attachment file name: {0} extension: {1}", fileNameWithExtension, extension);
        bool hasExtension = !string.IsNullOrEmpty(extension);
        if (!hasExtension)
        {
            throw new UserFriendlyException("Invalid file.");
        }

        bool isJsonFile = extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
        if (!isJsonFile)
        {
            // throw new UserFriendlyException("Invalid file. only support json file.");
        }
        
        bool isZipFile=extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
        if (isZipFile)
        {
            await UploadAppZipAttachmentAsync(file, appId, version);
            return;
        }
            
        string fileKey = Path.GetFileNameWithoutExtension(fileNameWithExtension); //Use file name as file key
        if (!Regex.IsMatch(fileKey, @"^[a-zA-Z0-9_-]+(\.[a-zA-Z0-9_-]+)*$"))
        {
            throw new UserFriendlyException("File name can only contain letters, numbers, underscores, hyphens, and dots (not at the start or end).");
        }
        
        using (Stream fileStream = file.OpenReadStream())
        {
            var s3FileName = GenerateAppAwsS3FileName(version, fileNameWithExtension);
            await _awsS3ClientService.UpLoadJsonFileAsync(fileStream, appId, s3FileName);
        }
        
        if (isJsonFile)
        {
            var appAttachmentGrain =
                _clusterClient.GetGrain<IAppAttachmentGrain>(
                    GrainIdHelper.GenerateAppAttachmentGrainId(appId, version));
            await appAttachmentGrain.AddAttachmentAsync(appId, version, fileKey,
                fileNameWithExtension, fileSize);
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