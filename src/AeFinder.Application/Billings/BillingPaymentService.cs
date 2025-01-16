using System;
using System.Threading.Tasks;
using AeFinder.Commons;
using AeFinder.Email;
using AeFinder.Options;
using AeFinder.User.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.Billings;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BillingPaymentService: AeFinderAppService, IBillingPaymentService
{
    private readonly IClusterClient _clusterClient;
    private readonly IBillingService _billingService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IBillingEmailSender _billingEmailSender;
    private readonly IBillingContractProvider _billingContractProvider;
    
    public BillingPaymentService(IClusterClient clusterClient,IBillingService billingService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IBillingEmailSender billingEmailSender,
        IBillingContractProvider billingContractProvider,
        IOptionsSnapshot<ContractOptions> contractOptions)
    {
        _clusterClient = clusterClient;
        _billingService = billingService;
        _organizationInformationProvider = organizationInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _billingEmailSender = billingEmailSender;
        _billingContractProvider = billingContractProvider;
    }

    public async Task RepayFailedBillingAsync(string organizationId, string billingId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        var billingGuid = Guid.Parse(billingId);

        var bill = await _billingService.GetAsync(organizationGuid, billingGuid);
        if (bill.Status != BillingStatus.Failed)
        {
            throw new UserFriendlyException("Only bills that have failed payment can be repaid");
        }
        
        //Get organization wallet address
        var organizationWalletAddress =
            await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
        if (string.IsNullOrEmpty(organizationWalletAddress))
        {
            throw new UserFriendlyException($"Organization wallet address is null or empty, please check.");
        }
        
        //Check user organization balance
        var userOrganizationBalanceInfoDto =
            await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                _contractOptions.BillingContractChainId, 0, 10);
        var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;
        if (organizationAccountBalance < bill.PaidAmount)
        {
            throw new UserFriendlyException(
                $"Organization wallet balance {organizationAccountBalance} is not enough to pay bill amount {bill.PaidAmount}.");
        }

        if (bill.Type == BillingType.AdvancePayment)
        {
            await SendLockFromContractTransactionAsync(bill, organizationWalletAddress);
        }

        if (bill.Type == BillingType.Settlement)
        {
            await SendChargeContractTransactionAsync(bill, organizationWalletAddress);
        }
    }

    private async Task SendLockFromContractTransactionAsync(BillingDto advancePaymentBill,
        string organizationWalletAddress)
    {
        //Send lockFrom transaction to contract
        var sendLockFromTransactionOutput = await _billingContractProvider.BillingLockFromAsync(
            organizationWalletAddress, advancePaymentBill.PaidAmount,
            advancePaymentBill.Id.ToString());
        Logger.LogInformation(
            $"[RepayFailedBillingAsync] Send lock from transaction " +
            sendLockFromTransactionOutput.TransactionId +
            " of bill " + advancePaymentBill.Id.ToString());
        var lockFromTransactionId = sendLockFromTransactionOutput.TransactionId;
        // not existed->retry  pending->wait  other->fail
        int delaySeconds = _contractOptions.DelaySeconds;
        var lockFromTransactionResult =
            await _billingContractProvider.QueryTransactionResultAsync(lockFromTransactionId, delaySeconds);
        var lockFromResultQueryTimes = 0;
        while (lockFromTransactionResult.Status == TransactionState.NotExisted &&
               lockFromResultQueryTimes < _contractOptions.ResultQueryRetryTimes)
        {
            lockFromResultQueryTimes++;

            await Task.Delay(delaySeconds);
            lockFromTransactionResult =
                await _billingContractProvider.QueryTransactionResultAsync(lockFromTransactionId, delaySeconds);
        }

        var lockFromTransactionStatus = lockFromTransactionResult.Status == TransactionState.Mined
            ? TransactionState.Mined
            : TransactionState.Failed;
        Logger.LogInformation(
            $"[RepayFailedBillingAsync] After {lockFromResultQueryTimes} times retry, get lock from transaction {lockFromTransactionId} status {lockFromTransactionStatus}");
        if (lockFromTransactionStatus == TransactionState.Mined)
        {
            await _billingService.PayAsync(advancePaymentBill.Id, lockFromTransactionId, DateTime.UtcNow);
            Logger.LogInformation($"[RepayFailedBillingAsync] Bill {advancePaymentBill.Id.ToString()} is paying.");
        }
        else
        {
            Logger.LogWarning($"[RepayFailedBillingAsync] Bill {advancePaymentBill.Id.ToString()} payment failed");
        }
    }

    private async Task SendChargeContractTransactionAsync(BillingDto settlementBill, string organizationWalletAddress)
    {
        //Send transaction to billing contract
        var sendChargeTransactionOutput = await _billingContractProvider.BillingChargeAsync(organizationWalletAddress,
            settlementBill.PaidAmount, settlementBill.RefundAmount,
            settlementBill.Id.ToString());
        Logger.LogInformation("[RepayFailedBillingAsync] Send charge transaction " +
                              sendChargeTransactionOutput.TransactionId +
                              " of bill " + settlementBill.Id.ToString());
        var chargeTransactionId = sendChargeTransactionOutput.TransactionId;
        // not existed->retry  pending->wait  other->fail
        int delaySeconds = _contractOptions.DelaySeconds;
        var chargeTransactionResult =
            await _billingContractProvider.QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
        var chargeResultQueryRetryTimes = 0;
        while (chargeTransactionResult.Status == TransactionState.NotExisted &&
               chargeResultQueryRetryTimes < _contractOptions.ResultQueryRetryTimes)
        {
            chargeResultQueryRetryTimes++;

            await Task.Delay(delaySeconds);
            chargeTransactionResult =
                await _billingContractProvider.QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
        }

        var chargeTransactionStatus = chargeTransactionResult.Status == TransactionState.Mined
            ? TransactionState.Mined
            : TransactionState.Failed;
        Logger.LogInformation(
            $"[RepayFailedBillingAsync] After {chargeResultQueryRetryTimes} times retry, get charge transaction {chargeTransactionId} status {chargeTransactionStatus}");
        if (chargeTransactionStatus == TransactionState.Mined)
        {
            await _billingService.PayAsync(settlementBill.Id, chargeTransactionId, DateTime.UtcNow);
            Logger.LogInformation($"[RepayFailedBillingAsync] Bill {settlementBill.Id.ToString()} is paying.");
        }
        else
        {
            Logger.LogError($"[RepayFailedBillingAsync] Bill {settlementBill.Id.ToString()} payment failed");
        }
    }

    public async Task<string> GetTreasurerAsync()
    {
        var address = await _billingContractProvider.GetTreasurerAsync();
        return address.ToBase58();
    }
}