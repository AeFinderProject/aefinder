using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.OpenIddict;
using AeFinder.Options;
using AeFinder.User.Dto;
using AeFinder.User.Provider;
using AElf;
using AElf.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace AeFinder;

public class SignatureGrantHandler: ITokenExtensionGrant, ITransientDependency
{
    private ILogger<SignatureGrantHandler> _logger;
    private IAbpDistributedLock _distributedLock;
    private const string LockKeyPrefix = "AeFinder:Auth:SignatureGrantHandler:";
    
    private SignatureOptions _signatureOptions;
    private ChainOptions _chainOptions;
    private IWalletLoginProvider _walletLoginProvider;
    private IUserInformationProvider _userInformationProvider;
    
    public string Name { get; } = SignatureGrantConsts.GrantType;

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var publicKeyVal = context.Request.GetParameter("publickey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var chainId = context.Request.GetParameter("chain_id").ToString();
        var caHash = context.Request.GetParameter("ca_hash").ToString();
        var timestampVal = context.Request.GetParameter("timestamp").ToString();
        var address = context.Request.GetParameter("address").ToString();
        var userName = context.Request.GetParameter("uname").ToString();

        //Before opening registration, must first log in with a regular account
        if (userName.IsNullOrEmpty())
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid user name.");
        }
        
        var invalidParamResult = CheckParams(publicKeyVal, signatureVal, chainId, address, timestampVal);
        if (invalidParamResult != null)
        {
            return invalidParamResult;
        }
        
        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var timestamp = long.Parse(timestampVal);
        var signAddress = string.Empty;
        if (!string.IsNullOrWhiteSpace(publicKeyVal))
        {
            signAddress = Address.FromPublicKey(publicKey).ToBase58();
        }

        _signatureOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<SignatureOptions>>()
            .CurrentValue;
        
        //Validate timestamp validity period
        if (_walletLoginProvider.IsTimeStampOutRange(timestamp, out int timeRange))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                $"The time should be {timeRange} minutes before and after the current time.");
        }

        _walletLoginProvider = context.HttpContext.RequestServices.GetRequiredService<IWalletLoginProvider>();
        _userInformationProvider = context.HttpContext.RequestServices.GetRequiredService<IUserInformationProvider>();
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        //Validate public key and signature
        if (!_walletLoginProvider.RecoverPublicKey(address, timestampVal, signature, out var managerPublicKey))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed new.");
        }

        if (!_walletLoginProvider.RecoverPublicKeyOld(address, timestampVal, signature, out var managerPublicKeyOld))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed old.");
        }

        if (!_walletLoginProvider.CheckPublicKey(managerPublicKey, managerPublicKeyOld, publicKeyVal))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid publicKey or signature.");
        }

        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
        _chainOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ChainOptions>>().CurrentValue;
        _logger.LogInformation(
            "publicKeyVal:{0}, signatureVal:{1}, address:{2}, caHash:{3}, chainId:{4}, timestamp:{5}",
            publicKeyVal, signatureVal, address, caHash, chainId, timestamp);

        
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        
        //Before opening registration, must first log in with a regular account
        var user = await userManager.FindByNameAsync(userName);
        if (user == null || user.IsDeleted)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, $"Invalid user name {userName}.");
        }

        //Add or update user extension info
        UserExtensionDto userExtensionDto = await _userInformationProvider.GetUserExtensionInfoByIdAsync(user.Id);
        userExtensionDto.UserId = user.Id;
        List<UserChainAddressDto> addressInfos;
        if (!string.IsNullOrWhiteSpace(caHash))
        {
            //If CA wallet connect
            if (!string.IsNullOrWhiteSpace(userExtensionDto.AElfAddress))
            {
                _logger.LogError(
                    "User has already linked a NightElf wallet; each user can only link one type of wallet. userExtensionAElfAddress:{0}, userId:{1}",
                    userExtensionDto.AElfAddress, user.Id);
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    "User has already linked a NightElf wallet; each user can only link one type of wallet.");
            }
            if (!string.IsNullOrWhiteSpace(userExtensionDto.CaHash) && userExtensionDto.CaHash != caHash)
            {
                _logger.LogError("User has already linked another Portkey wallet address. caHash:{0}, userExtensionCaHash:{1}, userId:{2}",
                    caHash, userExtensionDto.CaHash, user.Id);
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "User has already linked another Portkey wallet address.");
            }

            var managerCheck = await _walletLoginProvider.CheckAddressAsync(chainId, caHash, signAddress);
            if (!managerCheck.HasValue || !managerCheck.Value)
            {
                _logger.LogError("Manager validation failed. caHash:{0}, address:{1}, chainId:{2}",
                    caHash, address, chainId);
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Manager validation failed.");
            }

            addressInfos = await _walletLoginProvider.GetAddressInfosAsync(caHash);
            userExtensionDto.CaAddressList = addressInfos;
        }
        else
        {
            //If NightElf wallet connect
            if (address != signAddress)
            {
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid address or pubkey.");
            }
            if (!string.IsNullOrWhiteSpace(userExtensionDto.CaHash))
            {
                _logger.LogError(
                    "User has already linked a Portkey wallet; each user can only link one type of wallet. CaHash:{0}, userId:{1}",
                    userExtensionDto.CaHash, user.Id);
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    "User has already linked a Portkey wallet; each user can only link one type of wallet.");
            }
            if (!string.IsNullOrWhiteSpace(userExtensionDto.AElfAddress) && userExtensionDto.AElfAddress != signAddress)
            {
                _logger.LogError("User has already linked another NightElf wallet address. signAddress:{0}, userExtensionAElfAddress:{2}, userId:{3}",
                    signAddress, userExtensionDto.AElfAddress, user.Id);
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "User has already linked another NightElf wallet address.");
            }

            userExtensionDto.AElfAddress = signAddress;
        }
        
        caHash = string.IsNullOrWhiteSpace(caHash) ? string.Empty : caHash;
        userExtensionDto.CaHash = caHash;

        //Save user extension info to mongodb
        var saveUserExtensionResult = await _userInformationProvider.SaveUserExtensionInfoAsync(userExtensionDto);;
        if (!saveUserExtensionResult)
        {
            return GetForbidResult(OpenIddictConstants.Errors.ServerError, "Save user failed.");
        }
        
        var userClaimsPrincipalFactory = context.HttpContext.RequestServices
            .GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<IdentityUser>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<IdentityUser>>();
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
        claimsPrincipal.SetScopes(context.Request.GetScopes());
        claimsPrincipal.SetResources(await GetResourcesAsync(context, principal.GetScopes()));
        claimsPrincipal.SetAudiences("AeFinder");
        
        await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, principal);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }
    
    private ForbidResult CheckParams(string publicKeyVal, string signatureVal, string chainId, string address,
        string timestamp)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(publicKeyVal))
        {
            errors.Add("invalid parameter publish_key.");
        }

        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            errors.Add("invalid parameter signature.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            errors.Add("invalid parameter address.");
        }

        if (string.IsNullOrWhiteSpace(chainId))
        {
            errors.Add("invalid parameter chain_id.");
        }

        if (string.IsNullOrWhiteSpace(timestamp))
        {
            errors.Add("invalid parameter timestamp.");
        }

        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = GetErrorMessage(errors)
                }!));
        }

        return null;
    }
    
    private string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");
        if (message.Contains(','))
        {
            return message.TrimEnd().TrimEnd(',');
        }

        return message;
    }
    
    private ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }));
    }
    
    private async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context,
        ImmutableArray<string> scopes)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>()
                           .ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
    }

    private async Task<bool> CreateUserAsync(
        string caHash, UserExtensionDto userExtensionDto)
    {
        var result = false;
        await using var handle = await _distributedLock.TryAcquireAsync(name: LockKeyPrefix + caHash);
        if (handle != null)
        {
            //TODO Create user with wallet login
        }
        else
        {
            _logger.LogError("do not get lock, keys already exits, userId:{userId}",
                userExtensionDto.UserId.ToString());
        }

        return result;
    }
}