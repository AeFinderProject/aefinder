using System;
using System.Threading.Tasks;
using AeFinder.User.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.User;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserAppService : IdentityUserAppService, IUserAppService
{
    private readonly IOrganizationUnitRepository _organizationUnitRepository;
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public UserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        ILookupNormalizer lookupNormalizer,
        IOptions<IdentityOptions> identityOptions,
        IOpenIddictApplicationManager applicationManager,
        IOrganizationUnitRepository organizationUnitRepository)
        : base(userManager, userRepository, roleRepository, identityOptions)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _lookupNormalizer = lookupNormalizer;
        _applicationManager = applicationManager;
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

    public async Task RegisterAppAuthentication(string appId, string deployKey)
    {
        if (await _applicationManager.FindByClientIdAsync(appId) != null)
        {
            throw new Exception("A app with the same ID already exists.");
        }

        await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = appId,
            ClientSecret = deployKey,
            ConsentType=OpenIddictConstants.ConsentTypes.Implicit,
            Type = OpenIddictConstants.ClientTypes.Confidential,
            DisplayName = "AeFinder Apps",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Prefixes.Scope + "AeFinder",
                OpenIddictConstants.Permissions.ResponseTypes.IdToken
                
            }
        });
    }
}