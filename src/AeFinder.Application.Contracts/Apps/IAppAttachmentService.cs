using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AeFinder.Apps;

public interface IAppAttachmentService
{
    Task UploadAppAttachmentAsync(IFormFile file, string appId, string version);
    Task DeleteAppAttachmentAsync(string appId, string version, string fileKey);
}