using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Users;
using AeFinder.User;
using AeFinder.User.Dto;
using Orleans;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Xunit;

namespace AeFinder.Users;

public class UserAppServiceTests: AeFinderApplicationAppTestBase
{
    private readonly IUserAppService _userAppService;
    private readonly IdentityUserManager _userManager;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationUnitRepository _organizationUnitRepository;

    public UserAppServiceTests()
    {
        _userAppService = GetRequiredService<IUserAppService>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _organizationUnitRepository = GetRequiredService<IOrganizationUnitRepository>();
    }

    [Fact]
    public async Task Register_Test()
    {
        var registerUserInput = new RegisterUserInput
        {
            Email = "test@email.com",
            OrganizationName = "TestOrg",
            UserName = "TestUser",
            Password = "Asdf@123456"
        };
        await _userAppService.RegisterAsync(registerUserInput);

        var user = await _userManager.FindByEmailAsync(registerUserInput.Email);
        user.IsActive.ShouldBeFalse();
        user.EmailConfirmed.ShouldBeFalse();
        
        var codeGrain =
            _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
                GrainIdHelper.GenerateRegisterVerificationCodeGrainId(user.Email));
        var verificationCode = await codeGrain.GetCodeAsync();

        await _userAppService.RegisterConfirmAsync(verificationCode.Code);

        var org = await _organizationUnitRepository.GetAsync(registerUserInput.OrganizationName);
        org.DisplayName.ShouldBe(registerUserInput.OrganizationName);
        
        user = await _userManager.FindByEmailAsync(registerUserInput.Email);
        user.IsActive.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        user.OrganizationUnits.First().OrganizationUnitId.ShouldBe(org.Id);
    }
    
    [Fact]
    public async Task Register_Resend_Test()
    {
        var registerUserInput = new RegisterUserInput
        {
            Email = "test@email.com",
            OrganizationName = "TestOrg",
            UserName = "TestUser",
            Password = "Asdf@123456"
        };
        await _userAppService.RegisterAsync(registerUserInput);

        var user = await _userManager.FindByEmailAsync(registerUserInput.Email);
        user.IsActive.ShouldBeFalse();
        user.EmailConfirmed.ShouldBeFalse();
        
        var codeGrain =
            _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
                GrainIdHelper.GenerateRegisterVerificationCodeGrainId(user.Email));
        var oldCode = await codeGrain.GetCodeAsync();
        
        await _userAppService.ResendRegisterEmailAsync(new ResendEmailInput
        {
            Email = registerUserInput.Email
        });
        
        var newCode = await codeGrain.GetCodeAsync();
        newCode.Code.ShouldNotBe(oldCode.Code);

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _userAppService.RegisterConfirmAsync(oldCode.Code));

        await _userAppService.RegisterConfirmAsync(newCode.Code);

        var org = await _organizationUnitRepository.GetAsync(registerUserInput.OrganizationName);
        org.DisplayName.ShouldBe(registerUserInput.OrganizationName);
        
        user = await _userManager.FindByEmailAsync(registerUserInput.Email);
        user.IsActive.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        user.OrganizationUnits.First().OrganizationUnitId.ShouldBe(org.Id);
    }
}