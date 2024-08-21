using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AeFinder.Apps;

public interface IAppAttachmentService
{
    string GenerateAppJsonFileS3Key(string appId, string version, string fileName);
    Task UploadAppAttachmentAsync(IFormFile file, string appId, string version);
    Task DeleteAppAttachmentAsync(string appId, string version, string fileKey);
}