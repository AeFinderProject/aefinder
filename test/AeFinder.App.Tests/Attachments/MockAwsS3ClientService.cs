using System;
using System.IO;
using System.Threading.Tasks;
using AeFinder.AmazonCloud;

namespace AeFinder.App.Attachments;

public class MockAwsS3ClientService : IAwsS3ClientService
{
     public Task<string> UpLoadJsonFileAsync(Stream stream, string directory, string fileName)
    {
        string s3Key = GenerateJsonFileS3Key(directory, fileName);
        
        return Task.FromResult(s3Key);
    }

    public Task<string> GetJsonFileContentAsync(string directory, string fileName)
    {
        return Task.FromResult("{\"Info\": \"Hello World!\"}");
    }

    public Task DeleteJsonFileAsync(string directory, string fileName)
    {
        return Task.CompletedTask;
    }

    private string GenerateJsonFileS3Key(string directory, string fileName)
    {
        if (directory.IsNullOrEmpty())
        {
            return fileName + ".json";
        }
        return directory + "/" + fileName + ".json";
    }
}