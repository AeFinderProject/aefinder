using System;
using System.Threading.Tasks;
using AeFinder.User.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.User;

public class UserAppService : IdentityUserAppService, IUserAppService
{
    private readonly IOrganizationUnitRepository _organizationUnitRepository;
    private readonly ILookupNormalizer _lookupNormalizer;

    public UserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        ILookupNormalizer lookupNormalizer,
        IOptions<IdentityOptions> identityOptions,
        IOrganizationUnitRepository organizationUnitRepository)
        : base(userManager, userRepository, roleRepository, identityOptions)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _lookupNormalizer = lookupNormalizer;
    }

    public async Task<IdentityUserDto> RegisterUserWithOrganization(RegisterUserWithOrganizationInput input)
    {
        var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.Email, CurrentTenant.Id);

        if (input.OrganizationUnitId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Failed to create user. OrganizationUnitId is null");
        }

        var createResult = await UserManager.CreateAsync(user, input.Password);
        if (!createResult.Succeeded)
        {
            throw new UserFriendlyException("Failed to create user.");
        }
        
        //add appAdmin role into user
        var normalizedRoleName = _lookupNormalizer.NormalizeName(AeFinderConsts.AppAdminRoleName);
        var identityUser = await UserManager.FindByIdAsync(user.Id.ToString());
        var appAdminRole = await RoleRepository.FindByNormalizedNameAsync(normalizedRoleName);
        
        if (appAdminRole != null)
        {
            await UserManager.AddToRoleAsync(identityUser, appAdminRole.Name);
        }
        
        // bind organization with user
        var ou = await _organizationUnitRepository.GetAsync(input.OrganizationUnitId);
        await UserManager.AddToOrganizationUnitAsync(identityUser, ou);

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(identityUser);
    }
}