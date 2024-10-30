using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AeFinder.Options;
using AeFinder.User.Dto;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Contracts.CA;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace AeFinder.User.Provider;

public class UserInformationProvider: IUserInformationProvider, ISingletonDependency
{
    private readonly ILogger<UserInformationProvider> _logger;
    private readonly IRepository<IdentityUserExtension, Guid> _userExtensionRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly SignatureOptions _signatureOptions;
    private readonly ChainOptions _chainOptions;
    
    private const string GetHolderInfoMethodName = "GetHolderInfo";
    private const string PortKeyAppId = "PortKey";
    private const string NightElfAppId = "NightElf";
    private const string CrossChainContractName = "AElf.ContractNames.CrossChain";

    public UserInformationProvider(IRepository<IdentityUserExtension, Guid> userExtensionRepository,
        IObjectMapper objectMapper,ILogger<UserInformationProvider> logger,
        IOptionsMonitor<SignatureOptions> signatureOptions,IOptionsMonitor<ChainOptions> chainOptions)
    {
        _userExtensionRepository = userExtensionRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _signatureOptions = signatureOptions.CurrentValue;
        _chainOptions = chainOptions.CurrentValue;
    }

    public bool RecoverPublicKey(string address, string timestampVal, byte[] signature, out byte[] managerPublicKey)
    {
        var newSignText = """
                          Welcome to AeFinder! Click to sign in to the AeFinder platform! This request will not trigger any blockchain transaction or cost any gas fees.

                          signature: 
                          """ + string.Join("-", address, timestampVal);
        _logger.LogInformation("newSignText:{newSignText}", newSignText);
        return CryptoHelper.RecoverPublicKey(signature,
            HashHelper.ComputeFrom(Encoding.UTF8.GetBytes(newSignText).ToHex()).ToByteArray(),
            out managerPublicKey);
    }

    public bool RecoverPublicKeyOld(string address, string timestampVal, byte[] signature,
        out byte[] managerPublicKeyOld)
    {
        return CryptoHelper.RecoverPublicKey(signature,
            HashHelper.ComputeFrom(string.Join("-", address, timestampVal)).ToByteArray(),
            out managerPublicKeyOld);
    }

    public bool CheckPublicKey(byte[] managerPublicKey, byte[] managerPublicKeyOld, string publicKeyVal)
    {
        return (managerPublicKey.ToHex() == publicKeyVal || managerPublicKeyOld.ToHex() == publicKeyVal);
    }

    public async Task<bool?> CheckAddressAsync(string chainId, string graphQlUrl, string caHash, string manager)
    {
        var graphQlResult = await CheckAddressFromGraphQlAsync(graphQlUrl, caHash, manager);
        if (!graphQlResult.HasValue || !graphQlResult.Value)
        {
            _logger.LogDebug("graphql is invalid.");
            return await CheckAddressFromContractAsync(chainId, caHash, manager, _chainOptions);
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

    public async Task<List<UserChainAddressDto>> GetAddressInfosAsync(string caHash)
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

    public async Task<bool> SaveUserExtensionInfoAsync(UserExtensionDto userExtensionDto)
    {
        var userExtension = await _userExtensionRepository.FirstOrDefaultAsync(x => x.Id == userExtensionDto.UserId);
        if (userExtension == null)
        {
            // var caAddressMain=userExtensionDto.CaAddressList
            userExtension = new IdentityUserExtension(userExtensionDto.UserId)
            {
                UserId = userExtensionDto.UserId,
                AElfAddress = userExtensionDto.AElfAddress,
                CaHash = userExtensionDto.CaHash
            };
            if (userExtensionDto.CaAddressList != null && userExtensionDto.CaAddressList.Count > 0)
            {
                userExtension.CaAddressList =
                    _objectMapper.Map<List<UserChainAddressDto>, List<UserChainAddressInfo>>(userExtensionDto
                        .CaAddressList);
                var caAddressMain = userExtension.CaAddressList.FirstOrDefault(u => u.ChainId.ToUpper() == "AELF");
                userExtension.CaAddressMain = caAddressMain == null ? string.Empty : caAddressMain.Address;
            }

            await _userExtensionRepository.InsertAsync(userExtension);
            return true;
        }

        return false;
    }

    public async Task<UserExtensionDto> GetUserExtensionInfoByIdAsync(Guid userId)
    {
        var userExtension = await _userExtensionRepository.FindAsync(userId);
        if (userExtension == null)
        {
            return new UserExtensionDto();
        }
        return _objectMapper.Map<IdentityUserExtension,UserExtensionDto>(userExtension);
    }
}