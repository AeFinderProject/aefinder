using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Xunit;

namespace AeFinder.AppResources;

public class AppResourceServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodIndexRepository;
    private readonly IAppResourceService _appResourceService;

    public AppResourceServiceTests()
    {
        _appResourceService = GetRequiredService<IAppResourceService>();
        _appSubscriptionPodIndexRepository = GetRequiredService<IEntityMappingRepository<AppSubscriptionPodIndex, string>>();
    }

    [Fact]
    public async Task GetTest()
    {
        await _appSubscriptionPodIndexRepository.AddAsync(new AppSubscriptionPodIndex
        {
            AppId = "AppId",
            Version = "Version1",
            DockerImage = "DockerImage1"
        });
        
        await _appSubscriptionPodIndexRepository.AddAsync(new AppSubscriptionPodIndex
        {
            AppId = "AppId",
            Version = "Version2",
            DockerImage = "DockerImage2"
        });

        var resources = await _appResourceService.GetAsync("AppId");
        resources.Count().ShouldBe(2);
        resources.First(o=>o.Version=="Version1").DockerImage.ShouldBe("DockerImage1");
        resources.First(o=>o.Version=="Version2").DockerImage.ShouldBe("DockerImage2");
    }
}