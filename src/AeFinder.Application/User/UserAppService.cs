using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.User.Dto;
using AeFinder.User.Provider;
using AElf;
using AElf.Types;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization.Permissions;
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
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IWalletLoginProvider _walletLoginProvider;

    public UserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        ILookupNormalizer lookupNormalizer,
        IOptions<IdentityOptions> identityOptions,
        IOpenIddictApplicationManager applicationManager,
        IOrganizationAppService organizationAppService,
        IOrganizationUnitRepository organizationUnitRepository,
        IUserInformationProvider userInformationProvider,
        IWalletLoginProvider walletLoginProvider,
        IPermissionChecker permissionChecker)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _lookupNormalizer = lookupNormalizer;
        _applicationManager = applicationManager;
        _organizationAppService = organizationAppService;
        _userInformationProvider = userInformationProvider;
        _walletLoginProvider = walletLoginProvider;
    }

    public async Task<IdentityUserDto> RegisterUserWithOrganization(RegisterUserWithOrganizationInput input)
    {
        var userName = input.UserName.Trim();
        var email = input.Email.Trim();
        var existUser = await UserManager.FindByNameAsync(userName);
        if (existUser != null && !existUser.Id.ToString().IsNullOrEmpty())
        {
            throw new UserFriendlyException($"user {existUser.Name} is already exist!");
        }
        
        var user = new IdentityUser(GuidGenerator.Create(), userName, email, CurrentTenant.Id);

        if (input.OrganizationUnitId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Failed to create user. OrganizationUnitId is null");
        }

        Guid organizationUnitGuid;
        if (!Guid.TryParse(input.OrganizationUnitId, out organizationUnitGuid))
        {
            throw new UserFriendlyException("Invalid OrganizationUnitId string");
        }

        OrganizationUnitDto organizationUnitDto =
            await _organizationAppService.GetOrganizationUnitAsync(organizationUnitGuid);
        if (organizationUnitDto == null || organizationUnitDto.Id.ToString().IsNullOrEmpty())
        {
            throw new UserFriendlyException($"OrganizationUnit {organizationUnitGuid} is not exist");
        }

        var createResult = await UserManager.CreateAsync(user, input.Password);
        if (!createResult.Succeeded)
        {
            throw new UserFriendlyException("Failed to create user. " + createResult.Errors.Select(e => e.Description)
                .Aggregate((errors, error) => errors + ", " + error));
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
        var organizationUnit = await _organizationUnitRepository.GetAsync(organizationUnitDto.DisplayName);
        await UserManager.AddToOrganizationUnitAsync(identityUser, organizationUnit);
        await _organizationAppService.AddUserToOrganizationUnitAsync(identityUser.Id,organizationUnit.Id);

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(identityUser);
    }

    public async Task RegisterAppAuthentication(string appId, string deployKey)
    {
        if (await _applicationManager.FindByClientIdAsync(appId) != null)
        {
            throw new UserFriendlyException("A app with the same ID already exists.");
        }

        await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = appId,
            ClientSecret = deployKey,
            ConsentType=OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
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

    public async Task<IdentityUserExtensionDto> GetUserInfoAsync()
    {
        if (CurrentUser == null || CurrentUser.Id == null)
        {
            throw new UserFriendlyException("CurrentUser is null");
        }
        
        var identityUser = await UserManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (identityUser == null)
        {
            throw new UserFriendlyException("user not found.");
        }

        var identityUserExtensionDto = ObjectMapper.Map<IdentityUser, IdentityUserExtensionDto>(identityUser);
        var extensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(identityUser.Id);
        identityUserExtensionDto.userExtensionInfo = extensionInfo;
        return identityUserExtensionDto;
    }

    public async Task<string> GetClientDisplayNameAsync(string clientId)
    {
        var openIddictApplication = await _applicationManager.FindByClientIdAsync(clientId);
        // return openIddictApplication.DisplayName;
        
        var displayName = (string) openIddictApplication.GetType().GetProperty("DisplayName")?.GetValue(openIddictApplication);

        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        return string.Empty;
    }

    public async Task ResetPasswordAsync(string userName, string newPassword)
    {
        if (CurrentUser == null || CurrentUser.Id == null)
        {
            throw new UserFriendlyException("CurrentUser is null");
        }

        if (CurrentUser.UserName != userName)
        {
            throw new UserFriendlyException("Can only reset your own password");
        }

        var identityUser = await UserManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (identityUser == null)
        {
            throw new UserFriendlyException("user not found.");
        }

        var token = await UserManager.GeneratePasswordResetTokenAsync(identityUser);
        var result = await UserManager.ResetPasswordAsync(identityUser, token, newPassword);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException("reset user password failed." + result.Errors.Select(e => e.Description)
                .Aggregate((errors, error) => errors + ", " + error));
        }
    }

    public async Task<IdentityUserExtensionDto> BindUserWalletAsync(BindUserWalletInput input)
    {
        if (CurrentUser == null || CurrentUser.Id == null)
        {
            throw new UserFriendlyException("Please sign in first.");
        }
        
        var identityUser = await UserManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (identityUser == null)
        {
            throw new UserFriendlyException("user not found.");
        }
        
        // var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(input.SignatureVal);

        //Validate timestamp validity period
        if (_walletLoginProvider.IsTimeStampOutRange(input.Timestamp, out int timeRange))
        {
            throw new UserFriendlyException($"The time should be {timeRange} minutes before and after the current time.");
        }
        
        //Validate public key and signature
        if (!_walletLoginProvider.RecoverPublicKey(input.Address, input.Timestamp.ToString(), signature, out var publicKey))
        {
            throw new UserFriendlyException("Signature validation failed new.");
        }
        
        //If EOA wallet, signAddress is the wallet address; if CA wallet, signAddress is the manager address.
        var signAddress = Address.FromPublicKey(publicKey).ToBase58();
        
        //Add or update user extension info
        UserExtensionDto userExtensionDto = await _userInformationProvider.GetUserExtensionInfoByIdAsync(identityUser.Id);

        if (!userExtensionDto.WalletAddress.IsNullOrEmpty())
        {
            throw new UserFriendlyException("User has already linked a wallet.");
        }
        
        userExtensionDto.UserId = identityUser.Id;
        if (!string.IsNullOrWhiteSpace(input.CaHash))
        {
            //If CA wallet connect
            var managerCheck = await _walletLoginProvider.CheckManagerAddressAsync(input.ChainId, input.CaHash, signAddress);
            if (!managerCheck.HasValue || !managerCheck.Value)
            {
                Logger.LogError("Manager validation failed. caHash:{0}, address:{1}, chainId:{2}",
                    input.CaHash, input.Address, input.ChainId);
                throw new UserFriendlyException("Manager validation failed.");
            }
            
            List<UserChainAddressDto> addressInfos = await _walletLoginProvider.GetAddressInfosAsync(input.CaHash);
            if (addressInfos == null || addressInfos.Count == 0)
            {
                Logger.LogError("Get ca address failed. caHash:{0}, chainId:{1}",
                    input.CaHash, input.ChainId);
                throw new UserFriendlyException(OpenIddictConstants.Errors.InvalidRequest,
                    $"Can not get ca address in chain {input.ChainId}.");
            }
            var caAddress = addressInfos[0].Address;
            userExtensionDto.WalletAddress = caAddress;
        }
        else
        {
            //If NightElf wallet connect
            if (input.Address != signAddress)
            {
                throw new UserFriendlyException("Invalid address or signature.");
            }

            userExtensionDto.WalletAddress = signAddress;
        }

        //Save user extension info to mongodb
        var saveUserExtensionResult = await _userInformationProvider.SaveUserExtensionInfoAsync(userExtensionDto);;
        if (!saveUserExtensionResult)
        {
            throw new UserFriendlyException("Save user failed.");
        }

        var identityUserExtensionDto = ObjectMapper.Map<IdentityUser, IdentityUserExtensionDto>(identityUser);
        var extensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(identityUser.Id);
        identityUserExtensionDto.userExtensionInfo = extensionInfo;
        return identityUserExtensionDto;
    }
}