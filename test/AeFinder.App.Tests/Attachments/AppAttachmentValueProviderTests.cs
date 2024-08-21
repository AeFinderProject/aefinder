using System.Threading.Tasks;
using AeFinder.AmazonCloud;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
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

    public AppAttachmentValueProviderTests()
    {
        _awsS3ClientService = GetRequiredService<IAwsS3ClientService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _appInfoProvider = GetRequiredService<IAppInfoProvider>();
    }

    [Fact]
    public async Task InitTest()
    {
        var fileName = "testFileName.json";
        var key = "TestKey";
        var appAttachmentGrain = _clusterClient.GetGrain<IAppAttachmentGrain>(GrainIdHelper.GenerateAppAttachmentGrainId(_appInfoProvider.AppId, _appInfoProvider.Version));
        await appAttachmentGrain.AddAttachmentAsync(_appInfoProvider.AppId, _appInfoProvider.Version, key, fileName);
        var providers = ServiceProvider.GetServices<IAppAttachmentValueProvider>();
        foreach (var provider in providers)
        {
            await provider.InitValueAsync();
        }

        var appAttachmentValueProvider = GetRequiredService<IAppAttachmentValueProvider<TestInfo>>();
        var content = await _awsS3ClientService.GetJsonFileContentAsync(_appInfoProvider.AppId, fileName);
        var value = JsonConvert.DeserializeObject<TestInfo>(content);
        appAttachmentValueProvider.GetValue().Info.ShouldBe(value!.Info);
    }
}