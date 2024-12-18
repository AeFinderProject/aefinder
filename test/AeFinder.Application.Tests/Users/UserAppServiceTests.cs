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

        await _userAppService.RegisterConfirmAsync(verificationCode);

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
        newCode.ShouldNotBe(oldCode);

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _userAppService.RegisterConfirmAsync(oldCode));

        await _userAppService.RegisterConfirmAsync(newCode);

        var org = await _organizationUnitRepository.GetAsync(registerUserInput.OrganizationName);
        org.DisplayName.ShouldBe(registerUserInput.OrganizationName);
        
        user = await _userManager.FindByEmailAsync(registerUserInput.Email);
        user.IsActive.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        user.OrganizationUnits.First().OrganizationUnitId.ShouldBe(org.Id);
    }

    [Fact]
    public async Task Register_SameOrganization_Test()
    {
        var registerUserInput1 = new RegisterUserInput
        {
            Email = "test1@email.com",
            OrganizationName = "TestOrg",
            UserName = "TestUser1",
            Password = "Asdf@123456"
        };
        await _userAppService.RegisterAsync(registerUserInput1);
        
        var registerUserInput2 = new RegisterUserInput
        {
            Email = "test2@email.com",
            OrganizationName = "TestOrg",
            UserName = "TestUser2",
            Password = "Asdf@123456"
        };
        await _userAppService.RegisterAsync(registerUserInput2);
        
        var codeGrain =
            _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
                GrainIdHelper.GenerateRegisterVerificationCodeGrainId(registerUserInput1.Email));
        var verificationCode = await codeGrain.GetCodeAsync();

        await _userAppService.RegisterConfirmAsync(verificationCode);
        
        codeGrain =
            _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
                GrainIdHelper.GenerateRegisterVerificationCodeGrainId(registerUserInput2.Email));
        verificationCode = await codeGrain.GetCodeAsync();

        await Assert.ThrowsAsync<BusinessException>(async () =>
            await _userAppService.RegisterConfirmAsync(verificationCode));
        
        var user1 = await _userManager.FindByEmailAsync(registerUserInput1.Email);
        user1.IsActive.ShouldBeTrue();
        user1.EmailConfirmed.ShouldBeTrue();
        
        var user2 = await _userManager.FindByEmailAsync(registerUserInput2.Email);
        user2.IsActive.ShouldBeFalse();
        user2.EmailConfirmed.ShouldBeFalse();
    }
}