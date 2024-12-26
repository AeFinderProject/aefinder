using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.BlockScan;
using AeFinder.Common;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Market;
using AeFinder.Options;
using AeFinder.User.Provider;
using AElf.Client.Dto;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppDeleteHandler : IDistributedEventHandler<AppDeleteEto>, ITransientDependency
{
    private readonly ILogger<AppDeleteHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IAppOperationSnapshotProvider _appOperationSnapshotProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly TransactionPollingOptions _transactionPollingOptions;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;

    public AppDeleteHandler(ILogger<AppDeleteHandler> logger,
        IClusterClient clusterClient,
        IBlockScanAppService blockScanAppService,
        IContractProvider contractProvider,
        IUserInformationProvider userInformationProvider,
        IOrganizationInformationProvider organizationInformationProvider,
        IOptionsSnapshot<TransactionPollingOptions> transactionPollingOptions,
        IAppOperationSnapshotProvider appOperationSnapshotProvider,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _blockScanAppService = blockScanAppService;
        _contractProvider = contractProvider;
        _userInformationProvider = userInformationProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _appOperationSnapshotProvider = appOperationSnapshotProvider;
        _transactionPollingOptions = transactionPollingOptions.Value;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
    }
    
    public async Task HandleEventAsync(AppDeleteEto eventData)
    {
        var appId = eventData.AppId;
        var organizationId = eventData.OrganizationId;
        var organizationGrainId = await GetOrganizationGrainIdAsync(organizationId);
        //get renewal info
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        var oldOrderInfo =
            await ordersGrain.GetLatestPodResourceOrderAsync(organizationId, appId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
        var podResourceStartUseDay = await _appOperationSnapshotProvider.GetAppPodStartTimeAsync(appId);
        var subscriptionId = await renewalGrain.GetCurrentSubscriptionIdAsync(oldOrderInfo.OrderId);
        var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(subscriptionId);

        if (renewalInfo.IsActive)
        {
            //Cancel order
            await ordersGrain.CancelOrderByIdAsync(renewalInfo.OrderId);
            await renewalGrain.CancelRenewalByIdAsync(renewalInfo.SubscriptionId);
            
            if (renewalInfo.LastChargeDate.AddDays(1) < renewalInfo.NextRenewalDate)
            {
                //charge bill
                var latestLockedBill = await billsGrain.GetLatestLockedBillAsync(oldOrderInfo.OrderId);
                var lockedAmount = latestLockedBill.BillingAmount;
                var chargeFee =
                    await billsGrain.CalculatePodResourceMidWayChargeAmountAsync(renewalInfo, lockedAmount,
                        podResourceStartUseDay);
                var refundAmount = lockedAmount - chargeFee;

                //Send charge transaction to contract
                var userExtensionDto =
                    await _userInformationProvider.GetUserExtensionInfoByIdAsync(Guid.Parse(oldOrderInfo.UserId));
                if (userExtensionDto.WalletAddress.IsNullOrEmpty())
                {
                    throw new Exception("Please bind your user wallet first.");
                }

                var organizationWalletAddress =
                    await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,
                        userExtensionDto.WalletAddress);
                if (string.IsNullOrEmpty(organizationWalletAddress))
                {
                    _logger.LogError(
                        $"[AppDeleteHandler] The organization wallet address has not yet been linked to user {oldOrderInfo.UserId}");
                    throw new Exception("The organization wallet address has not yet been linked");
                }

                var oldChargeBill = await billsGrain.CreateChargeBillAsync(new CreateChargeBillDto()
                {
                    OrganizationId = organizationId,
                    OrderId = oldOrderInfo.OrderId,
                    SubscriptionId = subscriptionId,
                    ChargeFee = chargeFee,
                    Description = "User creates a new order and processes billing settlement for the existing order.",
                    RefundAmount = refundAmount
                });
                var sendChargeTransactionOutput = await _contractProvider.BillingChargeAsync(organizationWalletAddress,
                    oldChargeBill.BillingAmount, oldChargeBill.RefundAmount,
                    oldChargeBill.BillingId);
                _logger.LogInformation("[AppDeleteHandler] Send charge transaction " +
                                       sendChargeTransactionOutput.TransactionId +
                                       " of bill " + oldChargeBill.BillingId);
                var chargeTransactionId = sendChargeTransactionOutput.TransactionId;
                // not existed->retry  pending->wait  other->fail
                int delaySeconds = _transactionPollingOptions.DelaySeconds;
                var chargeTransactionResult = await QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
                var chargeResultQueryRetryTimes = 0;
                while (chargeTransactionResult.Status == TransactionState.NotExisted &&
                       chargeResultQueryRetryTimes < _transactionPollingOptions.RetryTimes)
                {
                    chargeResultQueryRetryTimes++;

                    await Task.Delay(delaySeconds);
                    chargeTransactionResult = await QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
                }

                var chargeTransactionStatus = chargeTransactionResult.Status == TransactionState.Mined
                    ? TransactionState.Mined
                    : TransactionState.Failed;
                await billsGrain.UpdateTransactionStatus(oldChargeBill.BillingId, chargeTransactionStatus);
                _logger.LogInformation(
                    $"[AppDeleteHandler] After {chargeResultQueryRetryTimes} times retry, get charge transaction {chargeTransactionId} status {chargeTransactionStatus}");

            }
        }
        
        //stop and destroy subscriptions
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.CurrentVersion != null && !allSubscriptions.CurrentVersion.Version.IsNullOrEmpty())
        {
            var currentVersion = allSubscriptions.CurrentVersion.Version;
            await _blockScanAppService.StopAsync(appId, currentVersion);
            _logger.LogInformation($"[AppDeleteHandler] the CurrentVersion {currentVersion} of App {appId} is stopped.");
        }

        if (allSubscriptions.PendingVersion != null && !allSubscriptions.PendingVersion.Version.IsNullOrEmpty())
        {
            var pendingVersion = allSubscriptions.PendingVersion.Version;
            await _blockScanAppService.StopAsync(appId, pendingVersion);
            _logger.LogInformation($"[AppDeleteHandler] the PendingVersion {pendingVersion} of App {appId} is stopped.");
        }
        
        
        var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(eventData.AppId);
        appInfoIndex.Status = eventData.Status;
        appInfoIndex.DeleteTime = eventData.DeleteTime;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
        _logger.LogInformation($"[AppDeleteHandler] App {eventData.AppId} is deleted.");
    }
    
    private async Task<string> GetOrganizationGrainIdAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        return organizationGuid.ToString("N");
    }
    
    private async Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, int delaySeconds)
    {
        // var transactionId = transaction.GetHash().ToHex();
        var transactionResult = await _contractProvider.GetBillingTransactionResultAsync(transactionId);
        while (transactionResult.Status == TransactionState.Pending)
        {
            await Task.Delay(delaySeconds);
            transactionResult = await _contractProvider.GetBillingTransactionResultAsync(transactionId);
        }

        return transactionResult;
    }
}