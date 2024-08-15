using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Validation;
using Xunit;

namespace AeFinder.Apps;

public class AppServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IAppService _appService;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appIndexRepository;
    private readonly IEntityMappingRepository<AppLimitInfoIndex, string> _appLimitIndexRepository;

    public AppServiceTests()
    {
        _appService = GetRequiredService<IAppService>();
        _appIndexRepository = GetRequiredService<IEntityMappingRepository<AppInfoIndex, string>>();
        _appLimitIndexRepository = GetRequiredService<IEntityMappingRepository<AppLimitInfoIndex, string>>();
    }

    [Fact]
    public async Task AppTest()
    {
        var createDto = new CreateAppDto
        {
            AppName = "a"
        };
        await Assert.ThrowsAsync<AbpValidationException>(async () => await _appService.CreateAsync(createDto));

        createDto.AppName = "qwertyuiop 1234567890";
        await Assert.ThrowsAsync<AbpValidationException>(async () => await _appService.CreateAsync(createDto));
        
        createDto.AppName = "abc#";
        await Assert.ThrowsAsync<AbpValidationException>(async () => await _appService.CreateAsync(createDto));
        
        createDto.AppName = "My App";
        var result = await _appService.CreateAsync(createDto);
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () => await _appService.GetAsync("appid"));

        var app = await _appService.GetAsync(result.AppId);
        app.AppId.ShouldBe("my_app");
        app.AppName.ShouldBe(createDto.AppName);
        app.Status.ShouldBe(AppStatus.UnDeployed);

        var updateDto = new UpdateAppDto
        {
            Description = "des",
            ImageUrl = "img",
            SourceCodeUrl = "github"
        };
        await Assert.ThrowsAsync<UserFriendlyException>(async () => await _appService.UpdateAsync("appid", updateDto));

        await _appService.UpdateAsync("my_app", updateDto);
        app = await _appService.GetAsync(result.AppId);
        app.Description.ShouldBe(updateDto.Description);
        app.ImageUrl.ShouldBe(updateDto.ImageUrl);
        app.SourceCodeUrl.ShouldBe(updateDto.SourceCodeUrl);
        
        createDto = new CreateAppDto
        {
            AppName = "My Test App"
        };
        await _appService.CreateAsync(createDto);

        var apps = await _appService.GetListAsync();
        apps.TotalCount.ShouldBe(2);
        apps.Items[0].AppId.ShouldBe("my_test_app");
        apps.Items[1].AppId.ShouldBe("my_app");
    }

    [Fact]
    public async Task AppIndex_Test()
    {
        for (int i = 0; i < 6; i++)
        {
            await _appIndexRepository.AddAsync(new AppInfoIndex
            {
                AppId = "AppId" + i,
                AppName = "App" + i,
                OrganizationId = "OrganizationId",
                OrganizationName = "OrganizationName",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Id = "AppId" + i
            });
        }

        var app = await _appService.GetIndexAsync("App");
        app.ShouldBeNull();
        
        app = await _appService.GetIndexAsync("AppId2");
        app.AppName.ShouldBe("App2");

        var apps = await _appService.GetIndexListAsync(new GetAppInput
        {
            AppId = "App",
            SkipCount = 0,
            MaxResultCount = 5
        });
        apps.Items.Count.ShouldBe(0);
        apps.TotalCount.ShouldBe(0);
        
        apps = await _appService.GetIndexListAsync(new GetAppInput
        {
            SkipCount = 0,
            MaxResultCount = 5
        });
        apps.Items.Count.ShouldBe(5);
        apps.TotalCount.ShouldBe(6);
    }

    [Fact]
    public async Task ResourceLimitIndex_Test()
    {
        var index = new AppLimitInfoIndex
        {
            AppId = "AppId",
            AppName = "AppName",
            OrganizationId = "OrganizationId",
            OrganizationName = "OrganizationName",
            OperationLimit = new OperationLimitInfo
            {
                MaxEntitySize = 1,
                MaxLogSize = 2,
                MaxContractCallCount = 3,
                MaxEntityCallCount = 4,
                MaxLogCallCount = 5
            },
            ResourceLimit = new ResourceLimitInfo
            {
                AppPodReplicas = 1,
                AppFullPodRequestMemory = "AppFullPodRequestMemory",
                AppQueryPodRequestMemory = "AppQueryPodRequestMemory",
                AppFullPodRequestCpuCore = "AppFullPodRequestCpuCore",
                AppQueryPodRequestCpuCore = "AppQueryPodRequestCpuCore"
            }
        };
        await _appLimitIndexRepository.AddAsync(index);

        var limit = await _appService.GetAppResourceLimitIndexListAsync(new GetAppResourceLimitInput());
        limit.TotalCount.ShouldBe(1);
        limit.Items.Count().ShouldBe(1);
        limit.Items[0].AppId.ShouldBe(index.AppId);
        limit.Items[0].AppName.ShouldBe(index.AppName);
        limit.Items[0].OrganizationId.ShouldBe(index.OrganizationId);
        limit.Items[0].OrganizationName.ShouldBe(index.OrganizationName);
        limit.Items[0].OperationLimit.MaxEntitySize.ShouldBe(index.OperationLimit.MaxEntitySize);
        limit.Items[0].OperationLimit.MaxLogSize.ShouldBe(index.OperationLimit.MaxLogSize);
        limit.Items[0].OperationLimit.MaxContractCallCount.ShouldBe(index.OperationLimit.MaxContractCallCount);
        limit.Items[0].OperationLimit.MaxEntityCallCount.ShouldBe(index.OperationLimit.MaxEntityCallCount);
        limit.Items[0].OperationLimit.MaxLogCallCount.ShouldBe(index.OperationLimit.MaxLogCallCount);
        limit.Items[0].ResourceLimit.AppPodReplicas.ShouldBe(index.ResourceLimit.AppPodReplicas);
        limit.Items[0].ResourceLimit.AppFullPodRequestMemory.ShouldBe(index.ResourceLimit.AppFullPodRequestMemory);
        limit.Items[0].ResourceLimit.AppQueryPodRequestMemory.ShouldBe(index.ResourceLimit.AppQueryPodRequestMemory);
        limit.Items[0].ResourceLimit.AppFullPodRequestCpuCore.ShouldBe(index.ResourceLimit.AppFullPodRequestCpuCore);
        limit.Items[0].ResourceLimit.AppQueryPodRequestCpuCore.ShouldBe(index.ResourceLimit.AppQueryPodRequestCpuCore);
    }
}