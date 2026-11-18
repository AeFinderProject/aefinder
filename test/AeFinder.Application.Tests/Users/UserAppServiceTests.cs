using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Users;
using AeFinder.User;
using AeFinder.User.Dto;
using Orleans;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Xunit;

namespace AeFinder.Users;

public class UserAppServiceTests: AeFinderApplicationTestBase
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchDelete_SoftDeletes_User_And_Preserves_Organization(bool confirmRegistration)
    {
        await _userAppService.RegisterAsync(new RegisterUserInput
        {
            Email = "delete-test@example.com",
            UserName = "DeleteTestUser",
            Password = "Asdf@123456"
        });
        var user = await _userManager.FindByNameAsync("DeleteTestUser");
        var codeGrain = _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
            GrainIdHelper.GenerateRegisterVerificationCodeGrainId(user.Email));
        var code = await codeGrain.GetCodeAsync();
        Guid? organizationId = null;
        if (confirmRegistration)
        {
            await _userAppService.RegisterConfirmAsync(code);
            user = await _userManager.FindByIdAsync(user.Id.ToString());
            organizationId = user.OrganizationUnits.Single().OrganizationUnitId;
        }

        var result = await _userAppService.BatchDeleteAsync(new BatchDeleteUsersInput
        {
            UserIds = new List<Guid> { user.Id }
        });

        result.DeletedUserIds.ShouldBe(new[] { user.Id });
        result.FailedUserIds.ShouldBeEmpty();
        (await _userManager.FindByIdAsync(user.Id.ToString())).ShouldBeNull();
        var organizationCount = await _organizationUnitRepository.GetCountAsync();
        var error = await Assert.ThrowsAsync<UserFriendlyException>(() => _userAppService.RegisterConfirmAsync(code));
        error.Message.ShouldBe("User information not found.");
        (await _organizationUnitRepository.GetCountAsync()).ShouldBe(organizationCount);
        if (organizationId.HasValue)
        {
            (await _organizationUnitRepository.FindAsync(organizationId.Value)).ShouldNotBeNull();
        }

        using (GetRequiredService<IDataFilter>().Disable<ISoftDelete>())
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var deletedUser = await _userManager.FindByIdAsync(user.Id.ToString());
                deletedUser.ShouldNotBeNull();
                deletedUser.IsDeleted.ShouldBeTrue();
                deletedUser.DeletionTime.ShouldNotBeNull();
            });
        }

        var retry = await _userAppService.BatchDeleteAsync(new BatchDeleteUsersInput
        {
            UserIds = new List<Guid> { user.Id }
        });
        retry.SkippedUserIds.ShouldBe(new[] { user.Id });
    }

    [Fact]
    public async Task Register_Test()
    {
        var registerUserInput = new RegisterUserInput
        {
            Email = "test@email.com",
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
        
        user = await _userManager.FindByEmailAsync(registerUserInput.Email);
        user.IsActive.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        user.OrganizationUnits.Count.ShouldBe(1);
        var org = await _organizationUnitRepository.GetAsync(user.OrganizationUnits.First().OrganizationUnitId);
        org.DisplayName.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task Register_Resend_Test()
    {
        var registerUserInput = new RegisterUserInput
        {
            Email = "test@email.com",
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
        
        user = await _userManager.FindByEmailAsync(registerUserInput.Email);
        user.IsActive.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        user.OrganizationUnits.Count.ShouldBe(1);
        var org = await _organizationUnitRepository.GetAsync(user.OrganizationUnits.First().OrganizationUnitId);
        org.DisplayName.ShouldNotBeEmpty();
    }
}
