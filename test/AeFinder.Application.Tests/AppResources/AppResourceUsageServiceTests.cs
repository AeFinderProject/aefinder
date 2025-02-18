using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AeFinder.AppResources.Dto;
using AeFinder.Apps;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Repositories;
using Elasticsearch.Net;
using Elasticsearch.Net.Specification.CatApi;
using Nest;
using Shouldly;
using Xunit;

namespace AeFinder.AppResources;

public class AppResourceUsageServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IEntityMappingRepository<AppResourceUsageIndex, string> _entityMappingRepository;
    private readonly IAppResourceUsageService _appResourceUsageService;

    public AppResourceUsageServiceTests()
    {
        _appResourceUsageService = GetRequiredService<IAppResourceUsageService>();
        _entityMappingRepository = GetRequiredService<IEntityMappingRepository<AppResourceUsageIndex, string>>();
    }

    [Fact]
    public async Task GetTest()
    {
        var appUsage1 = new AppResourceUsageDto
        {
            AppInfo = new AppInfoImmutable
            {
                AppId = "app1",
                AppName = "app1"
            },
            OrganizationId = AeFinderApplicationTestConsts.OrganizationId,
            ResourceUsages = new Dictionary<string, ResourceUsageDto>
            {
                { "version1", new ResourceUsageDto { StoreSize = 100 } },
                { "version2", new ResourceUsageDto { StoreSize = 200 } }
            }
        };
        await _appResourceUsageService.AddOrUpdateAsync(appUsage1);
        
        var appUsage2 = new AppResourceUsageDto
        {
            AppInfo = new AppInfoImmutable
            {
                AppId = "app2",
                AppName = "app2"
            },
            OrganizationId = AeFinderApplicationTestConsts.OrganizationId,
            ResourceUsages = new Dictionary<string, ResourceUsageDto>
            {
                { "version1", new ResourceUsageDto { StoreSize = 110 } },
                { "version2", new ResourceUsageDto { StoreSize = 210 } }
            }
        };
        await _appResourceUsageService.AddOrUpdateAsync(appUsage2);

        var list = await _appResourceUsageService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetAppResourceUsageInput());
        list.Items.Count.ShouldBe(2);
        list.Items.ShouldContain(o => o.AppInfo.AppId == "app1");
        list.Items.ShouldContain(o => o.AppInfo.AppId == "app2");
        
        list = await _appResourceUsageService.GetListAsync(null,
            new GetAppResourceUsageInput());
        list.Items.Count.ShouldBe(2);
        list.Items.ShouldContain(o => o.AppInfo.AppId == "app1");
        list.Items.ShouldContain(o => o.AppInfo.AppId == "app2");
        
        list = await _appResourceUsageService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetAppResourceUsageInput
            {
                AppId = "app1"
            });
        list.Items.Count.ShouldBe(1);
        list.Items[0].AppInfo.AppId.ShouldBe("app1");
        list.Items[0].ResourceUsages.Sum(o => o.Value.StoreSize).ShouldBe(300);

        await _appResourceUsageService.DeleteAsync("app1");
        
        list = await _appResourceUsageService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetAppResourceUsageInput
            {
            });
        list.Items.Count.ShouldBe(1);
        list.Items[0].AppInfo.AppId.ShouldBe("app2");
    }
}