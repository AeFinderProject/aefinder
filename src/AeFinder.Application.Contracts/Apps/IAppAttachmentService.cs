using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
namespace AeFinder.Apps;

public interface IAppAttachmentService
{
    // Task UploadAppAttachmentAsync(IFormFile file, string appId, string version);
    Task DeleteAppAttachmentAsync(string appId, string version, string fileKey);
    Task DeleteAllAppAttachmentsAsync(string appId, string version);
    Task<string> GetAppAttachmentContentAsync(string appId, string version, string fileKey);
    Task UploadAppAttachmentListAsync(List<IFormFile> attachmentList, string appId, string version);
}