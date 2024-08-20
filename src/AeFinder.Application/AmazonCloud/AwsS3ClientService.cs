using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AeFinder.AmazonCloud;

public class AwsS3ClientService : IAwsS3ClientService, ISingletonDependency
{
    private readonly AmazonS3Options _awsS3Option;
    private readonly ILogger<AmazonS3Options> _logger;

    private AmazonS3Client _amazonS3Client;
    
    public AwsS3ClientService(IOptionsSnapshot<AmazonS3Options> awsS3Option,ILogger<AmazonS3Options> logger)
    {
        _logger = logger;
        _awsS3Option = awsS3Option.Value;
        InitAmazonS3Client();
    }
    
    private void InitAmazonS3Client()
    {
        var accessKeyID = _awsS3Option.AccessKeyID;
        var secretKey = _awsS3Option.SecretKey;
        var ServiceURL = _awsS3Option.ServiceURL;
        var config = new AmazonS3Config()
        {
            ServiceURL = ServiceURL,
            RegionEndpoint = Amazon.RegionEndpoint.APNortheast1
        };
        _amazonS3Client = new AmazonS3Client(accessKeyID, secretKey, config);
    }

    public async Task<PutObjectResponse> UpLoadJsonFileAsync(Stream stream, string directory, string fileName)
    {
        string s3Key = await GenerateJsonFileS3Key(directory, fileName);
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = stream,
            BucketName = _awsS3Option.BucketName,
            Key = s3Key
            // CannedACL = S3CannedACL.PublicRead,
        };
        return await _amazonS3Client.PutObjectAsync(putObjectRequest);
    }

    public async Task<string> GenerateJsonFileS3Key(string directory, string fileName)
    {
        return directory + "/" + fileName + ".json";
    }

    public async Task<string> GetJsonFileS3Url(string directory, string fileName)
    {
        return $"https://{_awsS3Option.BucketName}.s3.amazonaws.com/{directory}/{fileName}.json";
    }
}