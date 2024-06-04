using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Validation;
using Xunit;

namespace AeFinder.Apps;

public class AppServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IAppService _appService;

    public AppServiceTests()
    {
        _appService = GetRequiredService<IAppService>();
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
}