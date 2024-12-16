using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Users;
using AeFinder.User.Dto;
using AeFinder.User.Provider;
using AElf;
using AElf.ExceptionHandler;
using AElf.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity;
using Volo.Abp.Timing;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.User;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public partial class UserAppService : IdentityUserAppService, IUserAppService
{
    private readonly IOrganizationUnitRepository _organizationUnitRepository;
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IWalletLoginProvider _walletLoginProvider;
    private readonly IClusterClient _clusterClient;

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
        IPermissionChecker permissionChecker,
        IClusterClient clusterClient)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _lookupNormalizer = lookupNormalizer;
        _applicationManager = applicationManager;
        _organizationAppService = organizationAppService;
        _userInformationProvider = userInformationProvider;
        _walletLoginProvider = walletLoginProvider;
        _clusterClient = clusterClient;
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
        identityUserExtensionDto.WalletAddress = extensionInfo.WalletAddress;
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
            Logger.LogInformation($"[ResetPasswordAsync] CurrentUser.UserName:{CurrentUser.UserName} userName:{userName}");
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

        var errors = _walletLoginProvider.CheckParams(input.Publickey, input.SignatureVal,
            input.ChainId, input.Address, input.Timestamp.ToString());
        if (errors.Count > 0)
        {
            throw new UserFriendlyException(_walletLoginProvider.GetErrorMessage(errors));
        }
        
        var wallectAddress = await VerifySignatureAndParseWalletAddressAsync(input.Publickey,
            input.SignatureVal, input.Timestamp.ToString(), input.CaHash, input.Address, input.ChainId);
        
        //Check if the wallet address is linked to another account
        UserExtensionDto userExtensionDto =
            await _userInformationProvider.GetUserExtensionInfoByWalletAddressAsync(wallectAddress);
        if (userExtensionDto != null)
        {
            throw new UserFriendlyException("This wallet address has already been linked to another account.");
        }
        
        //Add or update user extension info
        userExtensionDto = await _userInformationProvider.GetUserExtensionInfoByIdAsync(identityUser.Id);
        if (!userExtensionDto.WalletAddress.IsNullOrEmpty())
        {
            throw new UserFriendlyException("User has already linked a wallet.");
        }
        userExtensionDto.UserId = identityUser.Id;
        userExtensionDto.WalletAddress = wallectAddress;
        
        //Save user extension info to mongodb
        var saveUserExtensionResult = await _userInformationProvider.SaveUserExtensionInfoAsync(userExtensionDto);;
        if (!saveUserExtensionResult)
        {
            throw new UserFriendlyException("Save user failed.");
        }

        var identityUserExtensionDto = ObjectMapper.Map<IdentityUser, IdentityUserExtensionDto>(identityUser);
        var extensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(identityUser.Id);
        identityUserExtensionDto.WalletAddress = extensionInfo.WalletAddress;
        return identityUserExtensionDto;
    }
    
    public async Task RegisterAsync(RegisterUserInput input)
    {
        var userName = input.UserName.Trim();
        var email = input.Email.Trim();
        
        var orgName = input.OrganizationName.Trim();
        var existorganizationUnit = await _organizationUnitRepository.GetAsync(orgName);
        if (existorganizationUnit != null)
        {
            throw new UserFriendlyException("Organization already exists.");
        }

        var existUser = await UserManager.FindByNameAsync(userName);
        if (existUser != null)
        {
            throw new UserFriendlyException("User name already exists.");
        }
        
        var verificationCodeGrain =
            _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
                GrainIdHelper.GenerateRegisterVerificationCodeGrainId(email));
        var code = await verificationCodeGrain.GenerateCodeAsync();
        
        var user = new IdentityUser(GuidGenerator.Create(), userName, email, CurrentTenant.Id);
        user.SetIsActive(false);
        user.SetEmailConfirmed(false);

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
        
        var registerGrain = _clusterClient.GetGrain<IUserRegisterGrain>(GrainIdHelper.GenerateUserRegisterGrainId(code));
        await registerGrain.SetAsync(user.Id, input.OrganizationName);
        
        await SendRegisterEmailAsync(email, code);
    }

    public async Task RegisterConfirmAsync(string code)
    {
        var registerGrain =
            _clusterClient.GetGrain<IUserRegisterGrain>(GrainIdHelper.GenerateUserRegisterGrainId(code));
        var register = await registerGrain.GetAsync();
        if (register.OrganizationName.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Register information not found.");
        }
        
        var user = await UserManager.FindByIdAsync(register.UserId.ToString());
        if (user == null)
        {
            throw new UserFriendlyException("User information not found.");
        }
        
        var verificationCodeGrain =
            _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
                GrainIdHelper.GenerateRegisterVerificationCodeGrainId(user.Email));
        await verificationCodeGrain.VerifyAsync(code);
        
        user.SetEmailConfirmed(true);
        user.SetIsActive(true);
        await UserManager.UpdateAsync(user);
        
        await _organizationAppService.CreateOrganizationUnitAsync(register.OrganizationName);
        var organizationUnit = await _organizationUnitRepository.GetAsync(register.OrganizationName);
        await UserManager.AddToOrganizationUnitAsync(user, organizationUnit);
        await _organizationAppService.AddUserToOrganizationUnitAsync(user.Id,organizationUnit.Id);

        await verificationCodeGrain.RemoveAsync();
    }

    public async Task ResendRegisterEmailAsync(ResendEmailInput input)
    {
        var email = input.Email.Trim();
        var verificationCodeGrain =
            _clusterClient.GetGrain<IRegisterVerificationCodeGrain>(
                GrainIdHelper.GenerateRegisterVerificationCodeGrainId(email));
        var oldCode = await verificationCodeGrain.GetCodeAsync();
        if (oldCode.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Register information not found.");
        }
        
        var newCode = await verificationCodeGrain.GenerateCodeAsync();

        var registerGrain =
            _clusterClient.GetGrain<IUserRegisterGrain>(GrainIdHelper.GenerateUserRegisterGrainId(oldCode));
        var register = await registerGrain.GetAsync();

        registerGrain = _clusterClient.GetGrain<IUserRegisterGrain>(GrainIdHelper.GenerateUserRegisterGrainId(newCode));
        await registerGrain.SetAsync(register.UserId, register.OrganizationName);

        await SendRegisterEmailAsync(email, newCode);
    }

    private async Task SendRegisterEmailAsync(string email, string code)
    {
        // TODO: Send email
        Logger.LogInformation($"Temporary test: {email} - {code}");
    }

    [ExceptionHandler([typeof(SignatureVerifyException)], TargetType = typeof(UserAppService),
        MethodName = nameof(HandleSignatureVerifyExceptionAsync))]
    [ExceptionHandler(typeof(Exception), TargetType = typeof(UserAppService),
        MethodName = nameof(HandleExceptionAsync))]
    protected virtual async Task<string> VerifySignatureAndParseWalletAddressAsync(string publicKeyVal,
        string signatureVal, string timestampVal, string caHash, string address, string chainId)
    {
        return await _walletLoginProvider.VerifySignatureAndParseWalletAddressAsync(publicKeyVal, signatureVal,
            timestampVal, caHash, address, chainId);
    }
}