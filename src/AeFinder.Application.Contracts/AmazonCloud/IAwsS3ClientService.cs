using System.IO;
using System.Threading.Tasks;

namespace AeFinder.AmazonCloud;

public interface IAwsS3ClientService
{
    Task<string> UpLoadJsonFileAsync(Stream stream, string s3Key);

    Task<string> GetJsonFileAsync(string s3Key);

    Task DeleteJsonFileAsync(string s3Key);
}