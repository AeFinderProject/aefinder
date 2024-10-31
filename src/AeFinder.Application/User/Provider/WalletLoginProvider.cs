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

namespace AeFinder.User.Provider;

public class WalletLoginProvider: IWalletLoginProvider, ISingletonDependency
{
    private readonly ILogger<WalletLoginProvider> _logger;
    private readonly SignatureGrantOptions _signatureGrantOptions;
    private readonly ChainOptions _chainOptions;
    
    private const string GetHolderInfoMethodName = "GetHolderInfo";
    private const string PortKeyAppId = "PortKey";
    private const string NightElfAppId = "NightElf";
    private const string CrossChainContractName = "AElf.ContractNames.CrossChain";
    
    public WalletLoginProvider(ILogger<WalletLoginProvider> logger,
        IOptionsMonitor<SignatureGrantOptions> signatureOptions,IOptionsMonitor<ChainOptions> chainOptions)
    {
        _logger = logger;
        _signatureGrantOptions = signatureOptions.CurrentValue;
        _chainOptions = chainOptions.CurrentValue;
    }

    public bool IsTimeStampOutRange(long timestamp, out int timeRange)
    {
        var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        timeRange = _signatureGrantOptions.TimestampValidityRangeMinutes;
        if (time < DateTime.UtcNow.AddMinutes(-timeRange) ||
            time > DateTime.UtcNow.AddMinutes(timeRange))
        {
            return true;
        }

        return false;
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
    
    public async Task<bool?> CheckManagerAddressAsync(string chainId, string caHash, string manager)
    {
        string graphQlUrl = _signatureGrantOptions.PortkeyV2GraphQLUrl;
        var graphQlResult = await CheckManagerAddressFromGraphQlAsync(graphQlUrl, caHash, manager);
        if (!graphQlResult.HasValue || !graphQlResult.Value)
        {
            _logger.LogDebug("graphql is invalid.");
            return await CheckManagerAddressFromContractAsync(chainId, caHash, manager, _chainOptions);
        }

        return true;
    }
    
    private async Task<bool?> CheckManagerAddressFromContractAsync(string chainId, string caHash, string manager,
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
    
    private async Task<bool?> CheckManagerAddressFromGraphQlAsync(string url, string caHash,
        string managerAddress)
    {
        var cHolderInfos = await GetHolderInfosAsync(url, caHash);
        var loginChainHolderInfo =
            cHolderInfos.CaHolderInfo.Find(c => c.ChainId == _signatureGrantOptions.LoginChainId);
        var caHolderManagerInfos = loginChainHolderInfo?.ManagerInfos;
        return caHolderManagerInfos?.Any(t => t.Address == managerAddress);
    }
    
    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        bool isCrossChain, ChainOptions chainOptions) where T : class, IMessage<T>, new()
    {
        try
        {
            var chainInfo = chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.AElfNodeBaseUrl);
            await client.IsConnectedAsync();
            var address = client.GetAddressFromPrivateKey(_signatureGrantOptions.CommonPrivateKeyForCallTx);

            var contractAddress = isCrossChain
                ? (await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom(CrossChainContractName)))
                .ToBase58()
                : chainInfo.CAContractAddress;

            var transaction =
                await client.GenerateTransactionAsync(address, contractAddress,
                    methodName, param);

            var txWithSign = client.SignTransaction(_signatureGrantOptions.CommonPrivateKeyForCallTx, transaction);
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
        //Get CaAddress from portkey V2 graphql
        var holderInfoDto = await GetHolderInfosAsync(_signatureGrantOptions.PortkeyV2GraphQLUrl, caHash);

        var chainIds = new List<string>();
        if (holderInfoDto != null && !holderInfoDto.CaHolderInfo.IsNullOrEmpty())
        {
            addressInfos.AddRange(holderInfoDto.CaHolderInfo.Select(t => new UserChainAddressDto
                { ChainId = t.ChainId, Address = t.CaAddress }));
            chainIds = holderInfoDto.CaHolderInfo.Select(t => t.ChainId).ToList();
        }

        //Get CaAddress from node contract
        var chains = _chainOptions.ChainInfos.Select(key => _chainOptions.ChainInfos[key.Key])
            .Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId).Where(t => !chainIds.Contains(t));

        foreach (var chainId in chains)
        {
            if (chainId != _signatureGrantOptions.LoginChainId)
            {
                continue;
            }
            try
            {
                var addressInfo = await GetAddressInfoFromContractAsync(chainId, caHash);
                addressInfos.Add(addressInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "get holder from chain error, caHash:{caHash}", caHash);
            }
        }

        return addressInfos;
    }
    
    private async Task<UserChainAddressDto> GetAddressInfoFromContractAsync(string chainId, string caHash)
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
}