using System.IO;
using System.Threading.Tasks;

namespace AeFinder.AmazonCloud;

public interface IAwsS3ClientService
{
    Task<string> UpLoadJsonFileAsync(Stream stream, string directory, string fileName);

    Task<string> GetJsonFileAsync(string directory, string fileName);

    Task DeleteJsonFileAsync(string directory, string fileName);
}