using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Commons;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Billings;
using AeFinder.Grains.State.Billings;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Timing;

namespace AeFinder.Billings;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BillingManagementService : AeFinderAppService, IBillingManagementService
{
    private readonly IClusterClient _clusterClient;
    private readonly IBillingService _billingService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IBillingContractProvider _billingContractProvider;
    private readonly IClock _clock;
    private readonly ILogger<BillingManagementService> _logger;
    private readonly BillingOptions _billingOptions;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IUserAppService _userAppService;
    private readonly IBillingEmailSender _billingEmailSender;

    public BillingManagementService(IClusterClient clusterClient, IBillingService billingService,
        IOrganizationInformationProvider organizationInformationProvider,
        IBillingContractProvider billingContractProvider, ILogger<BillingManagementService> logger, IClock clock,
        IOptionsSnapshot<BillingOptions> billingOptions, IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions, IUserAppService userAppService,
        IBillingEmailSender billingEmailSender)
    {
        _clusterClient = clusterClient;
        _billingService = billingService;
        _organizationInformationProvider = organizationInformationProvider;
        _billingContractProvider = billingContractProvider;
        _logger = logger;
        _clock = clock;
        _indexerProvider = indexerProvider;
        _userAppService = userAppService;
        _billingEmailSender = billingEmailSender;
        _contractOptions = contractOptions.Value;
        _billingOptions = billingOptions.Value;
    }

    public async Task GenerateMonthlyBillingAsync(Guid organizationId, DateTime month)
    {
        month = month.ToMonthDate();
        var monthBillingGrain =
            _clusterClient.GetGrain<IMonthlyBillingGrain>(GrainIdHelper.GenerateGrainId(organizationId, month));
        var monthBilling = await monthBillingGrain.GetAsync();
        if (monthBilling.SettlementBillingId == Guid.Empty)
        {
            var settlementBilling = await GetBillingAsync(organizationId, BillingType.Settlement, month,
                month.AddMonths(1).AddSeconds(-1));
            if (settlementBilling == null)
            {
                settlementBilling = await _billingService.CreateAsync(organizationId, BillingType.Settlement, month);
            }

            await monthBillingGrain.CreateMonthlyBillingAsync(organizationId, month, settlementBilling.Id);
            return;
        }

        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(monthBilling.SettlementBillingId);
        var lastMonthSettlementBilling = await billingGrain.GetAsync();
        if (lastMonthSettlementBilling.Status == BillingStatus.Paid)
        {
            if (monthBilling.AdvancePaymentBillingId == Guid.Empty)
            {
                var advancePaymentBilling = await GetBillingAsync(organizationId, BillingType.Settlement,
                    month.AddMonths(1),
                    month.AddMonths(2).AddSeconds(-1));
                if (advancePaymentBilling == null)
                {
                    advancePaymentBilling =
                        await _billingService.CreateAsync(organizationId, BillingType.AdvancePayment, month);
                }

                await monthBillingGrain.SetAdvancePaymentBillingAsync(advancePaymentBilling.Id);
            }
        }
    }

    public async Task PayAsync(Guid billingId)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(billingId);
        var billing = await billingGrain.GetAsync();

        switch (billing.Status)
        {
            case BillingStatus.Unpaid:
                await PayBillingAsync(billing);
                break;
            case BillingStatus.Confirming:
                await HandleConfirmingBillingAsync(billing);
                break;
        }
    }

    public async Task RePayAsync(Guid billingId)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(billingId);
        var billing = await billingGrain.GetAsync();
        await PayBillingAsync(billing);
    }

    public async Task<bool> IsPaymentFailedAsync(Guid organizationId, DateTime month)
    {
        var monthBillingGrain =
            _clusterClient.GetGrain<IMonthlyBillingGrain>(GrainIdHelper.GenerateGrainId(organizationId, month));
        var monthBilling = await monthBillingGrain.GetAsync();

        // Billing failures are not checked because billing failures are not usually due to the user side.
        // if (monthBilling.SettlementBillingId != Guid.Empty)
        // {
        //     if (await IsPaymentFailedAsync(monthBilling.SettlementBillingId))
        //     {
        //         return true;
        //     }
        // }

        if (monthBilling.AdvancePaymentBillingId != Guid.Empty)
        {
            if (await IsPaymentFailedAsync(monthBilling.AdvancePaymentBillingId))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<BillingDto> GetBillingAsync(Guid organizationId, BillingType billingType, DateTime beginTime,
        DateTime endTime)
    {
        var bills = await _billingService.GetListAsync(organizationId, new GetBillingInput()
        {
            BeginTime = beginTime,
            EndTime = endTime,
            Type = billingType,
            SkipCount = 0,
            MaxResultCount = 1
        });

        return bills.Items.FirstOrDefault();
    }

    private async Task<bool> IsPaymentFailedAsync(Guid billingId)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(billingId);
        var billing = await billingGrain.GetAsync();
        if (billing.Status == BillingStatus.Failed)
        {
            return true;
        }

        return false;
    }

    private async Task PayBillingAsync(BillingState billing)
    {
        if (billing.PaidAmount == 0 && billing.RefundAmount == 0)
        {
            _logger.LogInformation(
                "Billing is confirmed(No need to pay). Id: {BillingId}, Type: {BillingType}, OrganizationId: {OrganizationId}",
                billing.Id, billing.Type, billing.OrganizationId);
            await _billingService.ConfirmPaymentAsync(billing.Id);
            return;
        }

        var organizationWalletAddress =
            await _organizationInformationProvider.GetOrganizationWalletAddressAsync(
                billing.OrganizationId.ToString());
        if (string.IsNullOrEmpty(organizationWalletAddress))
        {
            _logger.LogWarning("Organization: {Organization} unbind wallet.", billing.OrganizationId);
            return;
        }

        string txId;
        if (billing.Type == BillingType.Settlement)
        {
            txId = (await _billingContractProvider.BillingChargeAsync(organizationWalletAddress, billing.PaidAmount,
                billing.RefundAmount,
                billing.Id.ToString())).TransactionId;
        }
        else
        {
            txId = (await _billingContractProvider.BillingLockFromAsync(organizationWalletAddress, billing.PaidAmount,
                billing.Id.ToString())).TransactionId;
        }

        _logger.LogInformation(
            "Billing transaction sent. Id: {BillingId}, Type: {BillingType}, OrganizationId: {OrganizationId}, TxId: {TransactionId}",
            billing.Id, billing.Type, billing.OrganizationId, txId);
        await _billingService.PayAsync(billing.Id, txId, _clock.Now);
    }

    private async Task HandleConfirmingBillingAsync(BillingState billing)
    {
        var transactionResult = await _billingContractProvider.GetTransactionResultAsync(billing.TransactionId);
        if (transactionResult.Status == TransactionState.Mined)
        {
            var userFundRecordResult =
                await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null,
                    billing.Id.ToString(), 0, 1);
            if (userFundRecordResult == null || userFundRecordResult.UserFundRecord == null ||
                userFundRecordResult.UserFundRecord.Items.Count == 0)
            {
                return;
            }

            var userFundRecord = userFundRecordResult.UserFundRecord.Items.First();
            var libHeight = await _indexerProvider.GetCurrentVersionSyncLastIrreversibleBlockHeightsync();
            if (userFundRecord.Metadata.Block.BlockHeight < libHeight)
            {
                _logger.LogInformation(
                    "Billing is confirmed. Id: {BillingId}, Type: {BillingType}, OrganizationId: {OrganizationId}",
                    billing.Id, billing.Type, billing.OrganizationId);
                await _billingService.ConfirmPaymentAsync(billing.Id);

                // TODO: The email content is not friendly and needs to be optimized.
                var userInfo =
                    await _userAppService.GetDefaultUserInOrganizationUnitAsync(billing.OrganizationId);
                if (billing.Type == BillingType.Settlement)
                {

                    await _billingEmailSender.SendChargeBalanceSuccessfulNotificationAsync(userInfo.Email,
                        userFundRecord.Address, userFundRecord.Amount, userFundRecord.TransactionId
                    );
                }
                else
                {
                    await _billingEmailSender.SendLockBalanceSuccessfulNotificationAsync(userInfo.Email,
                        userFundRecord.Address, userFundRecord.Amount, userFundRecord.TransactionId
                    );
                }
            }
        }
        else
        {
            if (_clock.Now > billing.CreateTime.AddMinutes(_billingOptions.BillingOverdueMinutes) &&
                _clock.Now > billing.PaymentTime.AddMilliseconds(_billingOptions.PaymentWaitingMinutes))
            {
                _logger.LogInformation(
                    "Billing pay failed. Id: {BillingId}, Type: {BillingType}, OrganizationId: {OrganizationId}, TxId: {TransactionId}",
                    billing.Id, billing.Type, billing.OrganizationId, billing.TransactionId);
                await _billingService.PaymentFailedAsync(billing.Id);
                return;
            }

            if (_clock.Now > billing.PaymentTime.AddMilliseconds(_billingOptions.TransactionTimeoutMinutes))
            {
                _logger.LogInformation(
                    "Billing transaction timeout. Id: {BillingId}, Type: {BillingType}, OrganizationId: {OrganizationId}, TxId: {TransactionId}",
                    billing.Id, billing.Type, billing.OrganizationId, billing.TransactionId);
                await PayBillingAsync(billing);
            }
        }
    }
}