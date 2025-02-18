using System;
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
            ResourceUsages = new Dictionary<string, List<ResourceUsageDto>>
            {
                {
                    "version1", new List<ResourceUsageDto>
                    {
                        new ResourceUsageDto
                        {
                            Name = AeFinderApplicationConsts.AppStorageResourceName,
                            Limit = "200",
                            Usage = "100"
                        }
                    }
                },
                {
                    "version2", new List<ResourceUsageDto>
                    {
                        new ResourceUsageDto
                        {
                            Name = AeFinderApplicationConsts.AppStorageResourceName,
                            Limit = "400",
                            Usage = "200"
                        }
                    }
                }
            }
        };
        
        var appUsage2 = new AppResourceUsageDto
        {
            AppInfo = new AppInfoImmutable
            {
                AppId = "app2",
                AppName = "app2"
            },
            OrganizationId = AeFinderApplicationTestConsts.OrganizationId,
            ResourceUsages = new Dictionary<string, List<ResourceUsageDto>>
            {
                {
                    "version1", new List<ResourceUsageDto>
                    {
                        new ResourceUsageDto
                        {
                            Name = AeFinderApplicationConsts.AppStorageResourceName,
                            Limit = "2200",
                            Usage = "2100"
                        }
                    }
                },
                {
                    "version2", new List<ResourceUsageDto>
                    {
                        new ResourceUsageDto
                        {
                            Name = AeFinderApplicationConsts.AppStorageResourceName,
                            Limit = "600",
                            Usage = "300"
                        }
                    }
                }
            }
        };
        await _appResourceUsageService.AddOrUpdateAsync(new List<AppResourceUsageDto>
        {
            appUsage1,
            appUsage2
        });

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
        list.Items[0].ResourceUsages["version1"].Count().ShouldBe(1);
        list.Items[0].ResourceUsages["version1"][0].Name.ShouldBe(AeFinderApplicationConsts.AppStorageResourceName);
        list.Items[0].ResourceUsages["version1"][0].Limit.ShouldBe("200");
        list.Items[0].ResourceUsages["version1"][0].Usage.ShouldBe("100");
        list.Items[0].ResourceUsages["version2"].Count().ShouldBe(1);
        list.Items[0].ResourceUsages["version2"][0].Name.ShouldBe(AeFinderApplicationConsts.AppStorageResourceName);
        list.Items[0].ResourceUsages["version2"][0].Limit.ShouldBe("400");
        list.Items[0].ResourceUsages["version2"][0].Usage.ShouldBe("200");

        await _appResourceUsageService.DeleteAsync("app1");
        
        list = await _appResourceUsageService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetAppResourceUsageInput
            {
            });
        list.Items.Count.ShouldBe(1);
        list.Items[0].AppInfo.AppId.ShouldBe("app2");
    }
}