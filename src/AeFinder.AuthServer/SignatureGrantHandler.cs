using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Dto;
using AeFinder.Model;
using AeFinder.OpenIddict;
using AElf;
using AElf.Types;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
    private readonly string _lockKeyPrefix = "NFTMarketServer:Auth:SignatureGrantHandler:";
    private readonly string _source_portkey = "portkey";
    private readonly string _source_nightaelf = "nightElf";
    private readonly string _PortkeyV2 = "v2";
    
    public string Name { get; } = SignatureGrantConsts.GrantType;

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var publicKeyVal = context.Request.GetParameter("pubkey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var timestampVal = context.Request.GetParameter("timestamp").ToString();
        
        var accountInfo = context.Request.GetParameter("accountInfo").ToString();
        var source = context.Request.GetParameter("source").ToString();
        
        var invalidParamResult = CheckParams(publicKeyVal, signatureVal, timestampVal, accountInfo, source);
        if (invalidParamResult != null)
        {
            return invalidParamResult;
        }
        
        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var timestamp = long.Parse(timestampVal);
        var address = string.Empty;
        if (!string.IsNullOrWhiteSpace(publicKeyVal))
        {
            address = Address.FromPublicKey(publicKey).ToBase58();
        }
        
        var caHash = string.Empty;
        var caAddressMain = string.Empty;
        var caAddressSide = new Dictionary<string, string>();

        var signatureOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<SignatureOptions>>()
            .Value;
        if (source == _source_portkey)
        {
            var accountInfoList = JsonConvert.DeserializeObject<List<GrantAccountInfo>>(accountInfo);
            if(accountInfoList == null || accountInfoList.Count == 0)
            {
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    $"The accountInfo is invalid.");
            }
            
            //Find caHash by caAddress
            foreach (var account in accountInfoList)
            {
                var version = context.Request.GetParameter("version").ToString();
                var portkeyUrl = version == _PortkeyV2 ? signatureOptions.PortkeyV2GraphQLUrl : signatureOptions.PortkeyGraphQLUrl;
                var caHolderInfos = await GetCAHolderInfo(portkeyUrl,
                    new List<string>(){ account.Address} , account.ChainId);
                if (caHolderInfos == null || caHolderInfos.CaHolderManagerInfo==null || caHolderInfos.CaHolderManagerInfo.Count == 0)
                {
                    return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                        $"invalid caaddress {account.Address}.");
                }
                
                if (!string.IsNullOrEmpty(caHash) && caHash != caHolderInfos.CaHolderManagerInfo[0].CaHash)
                {
                    return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                        $"User identities are inconsistent.");
                }

                caHash = caHolderInfos.CaHolderManagerInfo[0].CaHash;
                if (account.ChainId.ToLower() == "aelf")
                {
                    caAddressMain = account.Address;
                }
                else
                {
                    caAddressSide.TryAdd(account.ChainId, account.Address);
                }
            }
        }
        else
        {
            var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
            var timeRange = signatureOptions.TimestampValidityRangeMinutes;
            if (time < DateTime.UtcNow.AddMinutes(-timeRange) ||
                time > DateTime.UtcNow.AddMinutes(timeRange))
            {
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    $"The time should be {timeRange} minutes before and after the current time.");
            }
        }
        
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        // _userInformationProvider = context.HttpContext.RequestServices.GetRequiredService<IUserInformationProvider>();

        var userName = address;
        if (!string.IsNullOrWhiteSpace(caHash))
        {
            userName = caHash;
        }
        
        var user = await userManager.FindByNameAsync(userName);
        if (user == null)
        {
            
        }
        else
        {
            
        }
        
        
        var userClaimsPrincipalFactory = context.HttpContext.RequestServices
            .GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<IdentityUser>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<IdentityUser>>();
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
        claimsPrincipal.SetScopes("NFTMarketServer");
        claimsPrincipal.SetResources(await GetResourcesAsync(context, principal.GetScopes()));
        claimsPrincipal.SetAudiences("NFTMarketServer");

        // await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimDestinationsManager>()
        //     .SetAsync(principal);
        await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, principal);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }
    
    private ForbidResult CheckParams(string publicKeyVal, string signatureVal, string timestampVal, string accoutInfo,
        string source)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(source))
        {
            errors.Add("invalid parameter source.");
        }

        if (source != _source_portkey && string.IsNullOrWhiteSpace(publicKeyVal))
        {
            errors.Add("invalid parameter pubkey.");
        }

        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            errors.Add("invalid parameter signature.");
        }

        if (source != _source_portkey && string.IsNullOrWhiteSpace(timestampVal) || !long.TryParse(timestampVal, out var time) || time <= 0)
        {
            errors.Add("invalid parameter timestamp.");
        }

        if (source == _source_portkey && string.IsNullOrWhiteSpace(accoutInfo))
        {
            errors.Add("invalid parameter account_info.");
        }

        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = GetErrorMessage(errors)
                }));
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
    
    private async Task<IndexerCAHolderInfos> GetCAHolderInfo(string url,List<string> userCaAddresses,string chainId)
    {
        using var graphQLClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());

        // It should just one item
        var graphQlRequest = new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderManagerInfo(dto: {chainId:$chainId, caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        chainId,caHash,caAddress}
                }",
            Variables = new
            {
                chainId=chainId,caAddresses = userCaAddresses, skipCount = 0, maxResultCount = userCaAddresses.Count
            }
        };

        var graphQLResponse = await graphQLClient.SendQueryAsync<IndexerCAHolderInfos>(graphQlRequest);

        return graphQLResponse.Data;
    }
}