using System.IO;
using System.Threading.Tasks;

namespace AeFinder.AmazonCloud;

public interface IAwsS3ClientService
{
    Task<string> UpLoadJsonFileAsync(Stream stream, string directory, string fileName);

    Task<string> GetJsonFileAsync(string directory, string fileName);

    Task DeleteJsonFileAsync(string s3Key);
    string GenerateJsonFileS3Key(string directory, string fileName);
    string GenerateAppAttachmentS3FileName(string appId, string version, string fileName);
}