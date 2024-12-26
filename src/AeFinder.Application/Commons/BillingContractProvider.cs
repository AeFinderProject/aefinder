using System;
using System.Threading.Tasks;
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

namespace AeFinder.Commons;

public interface IBillingContractProvider
{
    Task<SendTransactionOutput> BillingChargeAsync(string organizationWalletAddress, decimal chargeAmount,
        decimal unlockAmount, string billingId);

    Task<SendTransactionOutput> BillingLockFromAsync(string organizationWalletAddress, decimal lockAmount,
        string billingId);

    Task<TransactionResultDto> GetBillingTransactionResultAsync(string transactionId);
    Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, int delaySeconds);
}

public class BillingContractProvider
{
    private readonly ContractOptions _contractOptions;
    private readonly ILogger<BillingContractProvider> _logger;
    private readonly string TreasurerAccountPrivateKeyForCallTx;
    private readonly string SideChainNodeBaseUrl;
    private readonly string BillingContractAddress;
    
    public BillingContractProvider(ILogger<BillingContractProvider> logger,IOptionsMonitor<ContractOptions> contractOptionsMonitor)
    {
        _logger = logger;
        _contractOptions = contractOptionsMonitor.CurrentValue;
        SideChainNodeBaseUrl = _contractOptions.SideChainNodeBaseUrl;
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
        var client = new AElfClient(SideChainNodeBaseUrl);
        await client.IsConnectedAsync();
        
        var address = client.GetAddressFromPrivateKey(TreasurerAccountPrivateKeyForCallTx);
        try
        {
            _logger.LogInformation(
                "[BillingContractProvider]Generate tx methodName is: {methodName} From address is: {address} To address is {contractAddress}, Param is: {param}",
                methodName, address, contractAddress, param);
            var transaction =
                await client.GenerateTransactionAsync(address, contractAddress,
                    methodName, param);

            _logger.LogInformation("[BillingContractProvider]Send tx methodName iss: {methodName} param is: {transaction}", methodName, transaction);
            
            var txWithSign = client.SignTransaction(TreasurerAccountPrivateKeyForCallTx, transaction);

            var result = await client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError("[BillingContractProvider] Send Transaction error: " + e.Message.ToString(), e.StackTrace);
            Console.WriteLine(e);
            throw e;
        }
    }
    
    private async Task<T> CallTransactionAsync<T>(string methodName, IMessage param,
        string contractAddress) where T : class, IMessage<T>, new()
    {
        var client = new AElfClient(SideChainNodeBaseUrl);
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
        var client = new AElfClient(SideChainNodeBaseUrl);
        await client.IsConnectedAsync();
        
        var result = await client.GetTransactionResultAsync(transactionId);
        return result;
    }
    
    public async Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, int delaySeconds)
    {
        // var transactionId = transaction.GetHash().ToHex();
        var transactionResult = await GetBillingTransactionResultAsync(transactionId);
        while (transactionResult.Status == TransactionState.Pending)
        {
            await Task.Delay(delaySeconds);
            transactionResult = await GetBillingTransactionResultAsync(transactionId);
        }

        return transactionResult;
    }
}