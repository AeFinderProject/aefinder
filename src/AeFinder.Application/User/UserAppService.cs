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
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict.Applications;
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

    public async Task<IdentityUserExtensionDto> BindUserWalletAsync(string publicKeyVal, string signatureVal, string chainId,
        string caHash, long timestamp, string address)
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
        
        if (string.IsNullOrWhiteSpace(publicKeyVal))
        {
            throw new UserFriendlyException("invalid parameter publish_key.");
        }
        
        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            throw new UserFriendlyException("invalid parameter signature.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new UserFriendlyException("invalid parameter address.");
        }

        if (string.IsNullOrWhiteSpace(chainId))
        {
            throw new UserFriendlyException("invalid parameter chain_id.");
        }

        if (timestamp <= 0)
        {
            throw new UserFriendlyException("invalid parameter timestamp.");
        }
        
        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var signAddress = string.Empty;
        if (!string.IsNullOrWhiteSpace(publicKeyVal))
        {
            signAddress = Address.FromPublicKey(publicKey).ToBase58();
        }

        //Validate timestamp validity period
        if (_walletLoginProvider.IsTimeStampOutRange(timestamp, out int timeRange))
        {
            throw new UserFriendlyException($"The time should be {timeRange} minutes before and after the current time.");
        }
        
        //Validate public key and signature
        if (!_walletLoginProvider.RecoverPublicKey(address, timestamp.ToString(), signature, out var managerPublicKey))
        {
            throw new UserFriendlyException("Signature validation failed new.");
        }

        if (!_walletLoginProvider.RecoverPublicKeyOld(address, timestamp.ToString(), signature, out var managerPublicKeyOld))
        {
            throw new UserFriendlyException("Signature validation failed old.");
        }

        if (!_walletLoginProvider.CheckPublicKey(managerPublicKey, managerPublicKeyOld, publicKeyVal))
        {
            throw new UserFriendlyException("Invalid publicKey or signature.");
        }
        
        //Add or update user extension info
        UserExtensionDto userExtensionDto = await _userInformationProvider.GetUserExtensionInfoByIdAsync(identityUser.Id);
        userExtensionDto.UserId = identityUser.Id;
        List<UserChainAddressDto> addressInfos;
        if (!string.IsNullOrWhiteSpace(caHash))
        {
            //If CA wallet connect
            if (!string.IsNullOrWhiteSpace(userExtensionDto.AElfAddress))
            {
                Logger.LogError(
                    "User has already linked a NightElf wallet; each user can only link one type of wallet. userExtensionAElfAddress:{0}, userId:{1}",
                    userExtensionDto.AElfAddress, identityUser.Id);
                throw new UserFriendlyException(
                    "User has already linked a NightElf wallet; each user can only link one type of wallet.");
            }
            if (!string.IsNullOrWhiteSpace(userExtensionDto.CaHash) && userExtensionDto.CaHash != caHash)
            {
                Logger.LogError("User has already linked another Portkey wallet address. caHash:{0}, userExtensionCaHash:{1}, userId:{2}",
                    caHash, userExtensionDto.CaHash, identityUser.Id);
                throw new UserFriendlyException("User has already linked another Portkey wallet address.");
            }

            var managerCheck = await _walletLoginProvider.CheckAddressAsync(chainId, caHash, signAddress);
            if (!managerCheck.HasValue || !managerCheck.Value)
            {
                Logger.LogError("Manager validation failed. caHash:{0}, address:{1}, chainId:{2}",
                    caHash, address, chainId);
                throw new UserFriendlyException("Manager validation failed.");
            }

            addressInfos = await _walletLoginProvider.GetAddressInfosAsync(caHash);
            userExtensionDto.CaAddressList = addressInfos;
        }
        else
        {
            //If NightElf wallet connect
            if (address != signAddress)
            {
                throw new UserFriendlyException("Invalid address or pubkey.");
            }
            if (!string.IsNullOrWhiteSpace(userExtensionDto.CaHash))
            {
                Logger.LogError(
                    "User has already linked a Portkey wallet; each user can only link one type of wallet. CaHash:{0}, userId:{1}",
                    userExtensionDto.CaHash, identityUser.Id);
                throw new UserFriendlyException(
                    "User has already linked a Portkey wallet; each user can only link one type of wallet.");
            }
            if (!string.IsNullOrWhiteSpace(userExtensionDto.AElfAddress) && userExtensionDto.AElfAddress != signAddress)
            {
                Logger.LogError("User has already linked another NightElf wallet address. signAddress:{0}, userExtensionAElfAddress:{2}, userId:{3}",
                    signAddress, userExtensionDto.AElfAddress, identityUser.Id);
                throw new UserFriendlyException("User has already linked another NightElf wallet address.");
            }

            userExtensionDto.AElfAddress = signAddress;
        }
        
        caHash = string.IsNullOrWhiteSpace(caHash) ? string.Empty : caHash;
        userExtensionDto.CaHash = caHash;

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