using System;
using System.Threading.Tasks;
using AeFinder.Commons;
using AeFinder.Contracts;
using AeFinder.Options;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Common;

public interface IContractProvider
{
    Task<SendTransactionOutput> BillingChargeAsync(string organizationWalletAddress, decimal chargeAmount,
        decimal unlockAmount, string billingId);

    Task<SendTransactionOutput> BillingLockFromAsync(string organizationWalletAddress, decimal lockAmount,
        string billingId);

    Task<TransactionResultDto> GetBillingTransactionResultAsync(string transactionId);
}

public class ContractProvider: IContractProvider, ISingletonDependency
{
    private readonly ContractOptions _contractOptions;
    private readonly ILogger<ContractProvider> _logger;
    private readonly string TreasurerAccountPrivateKeyForCallTx;
    private readonly string AElfNodeBaseUrl;
    private readonly string BillingContractAddress;
    
    public ContractProvider(ILogger<ContractProvider> logger,IOptionsMonitor<ContractOptions> contractOptionsMonitor)
    {
        _contractOptions = contractOptionsMonitor.CurrentValue;
        _logger = logger;
        AElfNodeBaseUrl = _contractOptions.AElfNodeBaseUrl;
        BillingContractAddress = _contractOptions.BillingContractAddress;
        TreasurerAccountPrivateKeyForCallTx = _contractOptions.TreasurerAccountPrivateKeyForCallTx;
    }

    public async Task<SendTransactionOutput> BillingChargeAsync(string organizationWalletAddress, decimal chargeAmount,
        decimal unlockAmount, string billingId)
    {
        var param = new ChargeInput();
        param.Address = Address.FromBase58(organizationWalletAddress);
        param.Symbol = ContractConstant.USDT;
        param.BillingId = billingId;
        param.ChargeAmount = Convert.ToInt64(chargeAmount * ContractConstant.USDTDecimals);
        param.UnlockAmount = Convert.ToInt64(unlockAmount * ContractConstant.USDTDecimals);
        var contractTransaction = await SendTransactionAsync<ChargeInput>(AElfContractMethodName.BillingContractCharge,
            param, BillingContractAddress);
        _logger.LogInformation(
            $"[BillingChargeAsync] Send contract transaction {JsonConvert.SerializeObject(contractTransaction)} successfully.");
        return contractTransaction;
    }

    public async Task<SendTransactionOutput> BillingLockFromAsync(string organizationWalletAddress, decimal lockAmount,
        string billingId)
    {
        var param = new LockFromInput();
        param.Address = Address.FromBase58(organizationWalletAddress);
        param.Symbol = ContractConstant.USDT;
        param.Amount = Convert.ToInt64(lockAmount * ContractConstant.USDTDecimals);
        param.BillingId = billingId;
        var contractTransaction = await SendTransactionAsync<LockFromInput>(
            AElfContractMethodName.BillingContractLockFrom, param,
            BillingContractAddress);
        _logger.LogInformation(
            $"[BillingLockFromAsync] Send contract transaction {JsonConvert.SerializeObject(contractTransaction)} successfully.");
        return contractTransaction;
    }

    private async Task<SendTransactionOutput> SendTransactionAsync<T>(string methodName, IMessage param,string contractAddress)
        where T : class, IMessage<T>, new()
    {
        var client = new AElfClient(AElfNodeBaseUrl);
        await client.IsConnectedAsync();
        
        var address = client.GetAddressFromPrivateKey(TreasurerAccountPrivateKeyForCallTx);
        try
        {
            _logger.LogInformation(
                "[ContractProvider]Generate tx methodName is: {methodName} From address is: {address} To address is {contractAddress}, Param is: {param}",
                methodName, address, contractAddress, param);
            var transaction =
                await client.GenerateTransactionAsync(address, contractAddress,
                    methodName, param);

            _logger.LogInformation("[ContractProvider]Send tx methodName iss: {methodName} param is: {transaction}", methodName, transaction);
            
            var txWithSign = client.SignTransaction(TreasurerAccountPrivateKeyForCallTx, transaction);

            var result = await client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError("[ContractProvider] Send Transaction error: " + e.Message.ToString(), e.StackTrace);
            Console.WriteLine(e);
            throw e;
        }
    }

    private async Task<T> CallTransactionAsync<T>(string methodName, IMessage param,
        string contractAddress) where T : class, IMessage<T>, new()
    {
        var client = new AElfClient(AElfNodeBaseUrl);
        await client.IsConnectedAsync();
        var address = client.GetAddressFromPrivateKey(TreasurerAccountPrivateKeyForCallTx);

        var transaction =
            await client.GenerateTransactionAsync(address, contractAddress,
                methodName, param);

        _logger.LogDebug("Call tx methodName is: {methodName} param is: {transaction}", methodName, transaction);

        var txWithSign = client.SignTransaction(TreasurerAccountPrivateKeyForCallTx, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
        return value;
    }
    
    
    
    public async Task<TransactionResultDto> GetBillingTransactionResultAsync(string transactionId)
    {
        var client = new AElfClient(AElfNodeBaseUrl);
        await client.IsConnectedAsync();
        
        var result = await client.GetTransactionResultAsync(transactionId);
        return result;
    }
}