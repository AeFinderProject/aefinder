using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AeFinder.OpenIddict;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Dto;
using AeFinder.User.Provider;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Portkey.Contracts.CA;
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
    private const string GetHolderInfoMethodName = "GetHolderInfo";
    private const string PortKeyAppId = "PortKey";
    private const string NightElfAppId = "NightElf";
    private const string CrossChainContractName = "AElf.ContractNames.CrossChain";
    // private IClusterClient _clusterClient;
    private SignatureOptions _signatureOptions;
    private ChainOptions _chainOptions;
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
        var source = context.Request.GetParameter("source").ToString();
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
        var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        var timeRange = _signatureOptions.TimestampValidityRangeMinutes;
        if (time < DateTime.UtcNow.AddMinutes(-timeRange) ||
            time > DateTime.UtcNow.AddMinutes(timeRange))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                $"The time should be {timeRange} minutes before and after the current time.");
        }
        
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        //Validate public key and signature
        var newSignText = """
                          Welcome to AeFinder! Click to sign in to the AeFinder platform! This request will not trigger any blockchain transaction or cost any gas fees.

                          signature: 
                          """+string.Join("-", address, timestampVal);
        _logger.LogInformation("newSignText:{newSignText}",newSignText);
        if (!CryptoHelper.RecoverPublicKey(signature, HashHelper.ComputeFrom(Encoding.UTF8.GetBytes(newSignText).ToHex()).ToByteArray(),
                out var managerPublicKey))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed new.");
        }
        
        if (!CryptoHelper.RecoverPublicKey(signature, HashHelper.ComputeFrom(string.Join("-", address, timestampVal)).ToByteArray(),
                out var managerPublicKeyOld))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed old.");
        }

        if (!(managerPublicKey.ToHex() == publicKeyVal || managerPublicKeyOld.ToHex() == publicKeyVal))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid publicKey or signature.");
        }
        
        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
        // _clusterClient = context.HttpContext.RequestServices.GetRequiredService<IClusterClient>();
        _chainOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ChainOptions>>().CurrentValue;
        _logger.LogInformation(
            "publicKeyVal:{0}, signatureVal:{1}, address:{2}, caHash:{3}, chainId:{4}, timestamp:{5}",
            publicKeyVal, signatureVal, address, caHash, chainId, timestamp);

        
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        _userInformationProvider = context.HttpContext.RequestServices.GetRequiredService<IUserInformationProvider>();
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

            var managerCheck = await CheckAddressAsync(chainId, _signatureOptions.PortkeyV2GraphQLUrl, caHash, signAddress,
                _chainOptions);
            if (!managerCheck.HasValue || !managerCheck.Value)
            {
                _logger.LogError("Manager validation failed. caHash:{0}, address:{1}, chainId:{2}",
                    caHash, address, chainId);
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Manager validation failed.");
            }

            addressInfos = await GetAddressInfosAsync(caHash);
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
    
    
    private async Task<bool?> CheckAddressAsync(string chainId, string graphQlUrl, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var graphQlResult = await CheckAddressFromGraphQlAsync(graphQlUrl, caHash, manager);
        if (!graphQlResult.HasValue || !graphQlResult.Value)
        {
            _logger.LogDebug("graphql is invalid.");
            return await CheckAddressFromContractAsync(chainId, caHash, manager, chainOptions);
        }

        return true;
    }
    
    private async Task<bool?> CheckAddressFromContractAsync(string chainId, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            LoginGuardianIdentifierHash = Hash.Empty
        };

        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, GetHolderInfoMethodName, param, false,
                chainOptions);

        return output?.ManagerInfos?.Any(t => t.Address.ToBase58() == manager);
    }
    
    private async Task<bool?> CheckAddressFromGraphQlAsync(string url, string caHash,
        string managerAddress)
    {
        var cHolderInfos = await GetHolderInfosAsync(url, caHash);
        var caHolder = cHolderInfos?.CaHolderInfo?.SelectMany(t => t.ManagerInfos);
        return caHolder?.Any(t => t.Address == managerAddress);
    }
    
    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        bool isCrossChain, ChainOptions chainOptions) where T : class, IMessage<T>, new()
    {
        try
        {
            var chainInfo = chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.AElfNodeBaseUrl);
            await client.IsConnectedAsync();
            var address = client.GetAddressFromPrivateKey(_signatureOptions.CommonPrivateKeyForCallTx);

            var contractAddress = isCrossChain
                ? (await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom(CrossChainContractName)))
                .ToBase58()
                : chainInfo.CAContractAddress;

            var transaction =
                await client.GenerateTransactionAsync(address, contractAddress,
                    methodName, param);

            var txWithSign = client.SignTransaction(_signatureOptions.CommonPrivateKeyForCallTx, transaction);
            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var value = new T();
            value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
            return value;
        }
        catch (Exception e)
        {
            if (methodName != GetHolderInfoMethodName)
            {
                _logger.LogError(e, "CallTransaction error, chain id:{chainId}, methodName:{methodName}", chainId,
                    methodName);
            }

            _logger.LogError(e, "CallTransaction error, chain id:{chainId}, methodName:{methodName}", chainId,
                methodName);
            return null;
        }
    }
    
    private async Task<HolderInfoIndexerDto> GetHolderInfosAsync(string url, string caHash)
    {
        using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
        var request = new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData}}
                }",
            Variables = new
            {
                caHash, skipCount = 0, maxResultCount = 10
            }
        };

        var graphQlResponse = await graphQlClient.SendQueryAsync<HolderInfoIndexerDto>(request);
        return graphQlResponse.Data;
    }
    
    private async Task<List<UserChainAddressDto>> GetAddressInfosAsync(string caHash)
    {
        var addressInfos = new List<UserChainAddressDto>();
        var holderInfoDto = await GetHolderInfosAsync(_signatureOptions.PortkeyV2GraphQLUrl, caHash);

        var chainIds = new List<string>();
        if (holderInfoDto != null && !holderInfoDto.CaHolderInfo.IsNullOrEmpty())
        {
            addressInfos.AddRange(holderInfoDto.CaHolderInfo.Select(t => new UserChainAddressDto
                { ChainId = t.ChainId, Address = t.CaAddress }));
            chainIds = holderInfoDto.CaHolderInfo.Select(t => t.ChainId).ToList();
        }

        var chains = _chainOptions.ChainInfos.Select(key => _chainOptions.ChainInfos[key.Key])
            .Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId).Where(t => !chainIds.Contains(t));

        foreach (var chainId in chains)
        {
            try
            {
                var addressInfo = await GetAddressInfoAsync(chainId, caHash);
                addressInfos.Add(addressInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "get holder from chain error, caHash:{caHash}", caHash);
            }
        }

        return addressInfos;
    }
    
    private async Task<UserChainAddressDto> GetAddressInfoAsync(string chainId, string caHash)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            LoginGuardianIdentifierHash = Hash.Empty
        };

        var output = await CallTransactionAsync<GetHolderInfoOutput>(chainId, GetHolderInfoMethodName, param, false,
            _chainOptions);

        return new UserChainAddressDto()
        {
            Address = output.CaAddress.ToBase58(),
            ChainId = chainId
        };
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