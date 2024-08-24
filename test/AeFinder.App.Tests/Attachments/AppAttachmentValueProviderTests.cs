using System.Threading.Tasks;
using AeFinder.AmazonCloud;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Sdk.Attachments;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans;
using Shouldly;
using Xunit;

namespace AeFinder.App.Attachments;

public sealed class AppAttachmentValueProviderTests : AeFinderAppTestBase
{
    private readonly IAwsS3ClientService _awsS3ClientService;
    private readonly IClusterClient _clusterClient;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IAppAttachmentService _appAttachmentService;

    public AppAttachmentValueProviderTests()
    {
        _awsS3ClientService = GetRequiredService<IAwsS3ClientService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _appInfoProvider = GetRequiredService<IAppInfoProvider>();
        _appAttachmentService = GetRequiredService<IAppAttachmentService>();
    }

    [Fact]
    public async Task InitAppAttachmentValuesTest()
    {
        var fileName = "testFileName.json";
        var key = "TestKey";
        var appAttachmentGrain = _clusterClient.GetGrain<IAppAttachmentGrain>(GrainIdHelper.GenerateAppAttachmentGrainId(_appInfoProvider.AppId, _appInfoProvider.Version));
        await appAttachmentGrain.AddAttachmentAsync(_appInfoProvider.AppId, _appInfoProvider.Version, key, fileName,100000);
        var providers = ServiceProvider.GetServices<IAppAttachmentValueProvider>();
        foreach (var provider in providers)
        {
            var content =
                await _appAttachmentService.GetAppAttachmentContentAsync(_appInfoProvider.AppId,
                    _appInfoProvider.Version, provider.Key);
            provider.InitValue(content);
        }

        var appAttachmentValueProvider = GetRequiredService<IAppAttachmentValueProvider<TestInfo>>();
        var fileContent = await _awsS3ClientService.GetJsonFileContentAsync(_appInfoProvider.AppId, fileName);
        var value = JsonConvert.DeserializeObject<TestInfo>(fileContent);
        appAttachmentValueProvider.GetValue().Info.ShouldBe(value!.Info);
    }
}