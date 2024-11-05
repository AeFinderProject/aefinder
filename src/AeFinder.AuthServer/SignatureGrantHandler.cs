using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.OpenIddict;
using AeFinder.User;
using AeFinder.User.Dto;
using AeFinder.User.Provider;
using AElf;
using AElf.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        
        _walletLoginProvider = context.HttpContext.RequestServices.GetRequiredService<IWalletLoginProvider>();
        _userInformationProvider = context.HttpContext.RequestServices.GetRequiredService<IUserInformationProvider>();
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();

        var errors = _walletLoginProvider.CheckParams(publicKeyVal, signatureVal, chainId, address, timestampVal);
        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = _walletLoginProvider.GetErrorMessage(errors)
                }!));
        }

        string wallectAddress = string.Empty;
        UserExtensionDto userExtensionDto = null;
        try
        {
            wallectAddress = await _walletLoginProvider.VerifySignatureAndParseWalletAddressAsync(publicKeyVal,
                signatureVal, timestampVal, caHash, address, chainId);
        }
        catch (SignatureVerifyException verifyException)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                verifyException.Message);
        }
        catch (Exception e)
        {
            _logger.LogError("[SignatureGrantHandler] Signature validation failed: {e}",
                e.Message);
            throw;
        }
        
        userExtensionDto = await _userInformationProvider.GetUserExtensionInfoByWalletAddressAsync(wallectAddress);
        if (userExtensionDto == null)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                $"Invalid user, please register an account first,then bind your wallet.");
        }
        
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        var user = await userManager.FindByIdAsync(userExtensionDto.UserId.ToString());
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