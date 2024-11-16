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
        IOptionsMonitor<SignatureGrantOptions> signatureOptions, IOptionsMonitor<ChainOptions> chainOptions)
    {
        _logger = logger;
        _signatureGrantOptions = signatureOptions.CurrentValue;
        _chainOptions = chainOptions.CurrentValue;
    }

    public List<string> CheckParams(string publicKeyVal, string signatureVal, string chainId, string address,
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

        return errors;
    }

    public async Task<string> VerifySignatureAndParseWalletAddressAsync(string publicKeyVal, string signatureVal,
        string timestampVal, string caHash, string address, string chainId)
    {
        var timestamp = long.Parse(timestampVal);

        //Validate timestamp validity period
        if (IsTimeStampOutRange(timestamp, out int timeRange))
        {
            throw new SignatureVerifyException(
                $"The time should be {timeRange} minutes before and after the current time.");
        }

        //Validate public key and signature
        var signAddress = VerifySignature(address, timestampVal, signatureVal, publicKeyVal);

        //If EOA wallet, signAddress is the wallet address; if CA wallet, signAddress is the manager address.
        _logger.LogInformation(
            "[VerifySignature] signatureVal:{1}, address:{2}, signAddress:{3}, caHash:{4}, chainId:{5}, timestamp:{6}",
            signatureVal, address, signAddress, caHash, chainId, timestamp);

        if (!string.IsNullOrWhiteSpace(caHash))
        {
            //If CA wallet connect
            var managerCheck = await CheckManagerAddressAsync(chainId, caHash, signAddress);
            if (!managerCheck.HasValue || !managerCheck.Value)
            {
                _logger.LogError(
                    "[VerifySignature] Manager validation failed. caHash:{0}, address:{1}, chainId:{2}",
                    caHash, address, chainId);
                throw new SignatureVerifyException("Manager validation failed.");
            }

            List<UserChainAddressDto> addressInfos = await GetAddressInfosAsync(caHash);
            if (addressInfos == null || addressInfos.Count == 0)
            {
                _logger.LogError("[VerifySignature] Get ca address failed. caHash:{0}, chainId:{1}",
                    caHash, chainId);
                throw new SignatureVerifyException($"Can not get ca address in chain {chainId}.");
            }

            var caAddress = addressInfos[0].Address;
            return caAddress;
        }
        else
        {
            //If NightElf wallet connect
            if (address != signAddress)
            {
                throw new SignatureVerifyException("Invalid address or signature.");
            }

            return signAddress;
        }
    }

    private string VerifySignature(string address, string timestampVal, string signatureVal,string publicKeyVal)
    {
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        
        //Portkey discover wallet signature
        if (!RecoverPublicKey(address, timestampVal, signature, out var managerPublicKey))
        {
            throw new SignatureVerifyException("Signature validation failed new.");
        }

        //EOA/PortkeyAA wallet signature
        if (!RecoverPublicKeyOld(address, timestampVal, signature, out var managerPublicKeyOld))
        {
            throw new SignatureVerifyException("Signature validation failed old.");
        }
        
        if (!(managerPublicKey.ToHex() == publicKeyVal || managerPublicKeyOld.ToHex() == publicKeyVal))
        {
            throw new SignatureVerifyException("Invalid publicKey or signature.");
        }
        
        //Since it is not possible to determine whether the CA wallet manager address is in managerPublicKey or in managerPublicKeyOld
        //therefore, the accurate manager address is obtained from publicKeyVal.
        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signAddress = Address.FromPublicKey(publicKey).ToBase58();
        return signAddress;
    }

    public string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");
        if (message.Contains(','))
        {
            return message.TrimEnd().TrimEnd(',');
        }

        return message;
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

    private bool RecoverPublicKey(string address, string timestampVal, byte[] signature, out byte[] managerPublicKey)
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

    private bool RecoverPublicKeyOld(string address, string timestampVal, byte[] signature, out byte[] managerPublicKeyOld)
    {
        var oldSignText = string.Join("-", address, timestampVal);
        _logger.LogInformation("oldSignText:{oldSignText}", oldSignText);
        return CryptoHelper.RecoverPublicKey(signature,
            HashHelper.ComputeFrom(string.Join("-", address, timestampVal)).ToByteArray(),
            out managerPublicKeyOld);
    }
    
    private async Task<bool?> CheckManagerAddressAsync(string chainId, string caHash, string manager)
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
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, GetHolderInfoMethodName, param, 
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
        ChainOptions chainOptions) where T : class, IMessage<T>, new()
    {
        try
        {
            var chainInfo = chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.AElfNodeBaseUrl);
            await client.IsConnectedAsync();
            var address = client.GetAddressFromPrivateKey(_signatureGrantOptions.CommonPrivateKeyForCallTx);

            var contractAddress = chainInfo.CAContractAddress;

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
        //Get CaAddress from portkey V2 graphql
        var holderInfoDto = await GetHolderInfosAsync(_signatureGrantOptions.PortkeyV2GraphQLUrl, caHash);

        var chainIds = new List<string>();
        if (holderInfoDto != null && !holderInfoDto.CaHolderInfo.IsNullOrEmpty())
        {
            addressInfos.AddRange(holderInfoDto.CaHolderInfo
                .Select(t => new UserChainAddressDto { ChainId = t.ChainId, Address = t.CaAddress }));
            chainIds = holderInfoDto.CaHolderInfo.Select(t => t.ChainId).ToList();
        }

        //Get CaAddress from node contract
        var chains = _chainOptions.ChainInfos.Select(key => _chainOptions.ChainInfos[key.Key])
            .Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId).Where(t => !chainIds.Contains(t));

        foreach (var chainId in chains)
        {
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

        var output = await CallTransactionAsync<GetHolderInfoOutput>(chainId, GetHolderInfoMethodName, param, 
            _chainOptions);

        return new UserChainAddressDto()
        {
            Address = output.CaAddress.ToBase58(),
            ChainId = chainId
        };
    }
}