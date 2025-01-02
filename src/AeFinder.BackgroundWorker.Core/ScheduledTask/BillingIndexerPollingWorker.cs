using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Merchandises;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class BillingIndexerPollingWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<BillingIndexerPollingWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IBillingService _billingService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IAssetService _assetService;
    private readonly IAppDeployService _appDeployService;
    private readonly IBillingContractProvider _billingContractProvider;
    private readonly IUserAppService _userAppService;
    private readonly IUserInformationProvider _userInformationProvider;
    
    public BillingIndexerPollingWorker(AbpAsyncTimer timer, 
        ILogger<BillingIndexerPollingWorker> logger, IClusterClient clusterClient, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IBillingService billingService,IAssetService assetService,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppDeployService appDeployService,
        IBillingContractProvider billingContractProvider,
        IUserAppService userAppService,
        IUserInformationProvider userInformationProvider,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _organizationInformationProvider = organizationInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _billingService = billingService;
        _assetService = assetService;
        _graphQlOptions = graphQlOptions.Value;
        _appDeployService = appDeployService;
        _billingContractProvider = billingContractProvider;
        _userAppService = userAppService;
        _userInformationProvider = userInformationProvider;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.BillingIndexerPollingTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessIndexerPollingAsync();
    }

    private async Task ProcessIndexerPollingAsync()
    {
        _logger.LogInformation("[BillingIndexerPollingWorker] Process indexer polling Async.");
        
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            
            //Check organization wallet address is bind
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                var users = await _userAppService.GetUsersInOrganizationUnitAsync(organizationUnitDto.Id);
                if (users == null)
                {
                    _logger.LogWarning($"No users under the organization {organizationName}");
                    continue;
                }

                var defaultUser = users.FirstOrDefault();
                var userExtensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(defaultUser.Id);
                if (string.IsNullOrEmpty(userExtensionInfo.WalletAddress))
                {
                    _logger.LogWarning($"The user {defaultUser.Id} has not yet linked a wallet address.");
                    continue;
                }

                organizationWalletAddress =
                    await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,
                        userExtensionInfo.WalletAddress);
                if (string.IsNullOrEmpty(organizationWalletAddress))
                {
                    _logger.LogWarning($"Organization {organizationId} wallet address is null or empty, please check.");
                    continue;
                }
            }

            //Handle advance payment bills
            var advancePaymentBills =
                await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.AdvancePayment);
            foreach (var advancePaymentBill in advancePaymentBills)
            {
                await HandleAdvancePaymentBillAsync(advancePaymentBill);
            }
            
            //Handle settlement bills
            var settlementBills = await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.Settlement);
            foreach (var settlementBill in settlementBills)
            {
                await HandleSettlementBillAsync(organizationUnitDto.Id, settlementBill);
            }

        }
        
    }

    private async Task HandleAdvancePaymentBillAsync(BillingDto advancePaymentBill)
    {
        if (advancePaymentBill.Status == BillingStatus.Paid)
        {
            return;
        }

        //Automatically cancel bills that have remained unpaid for a long time
        if (advancePaymentBill.Status == BillingStatus.Unpaid)
        {
            if (advancePaymentBill.CreateTime.AddMinutes(_scheduledTaskOptions.UnpaidBillTimeoutMinutes) <
                DateTime.UtcNow)
            {
                //TODO Set bill status to failed
            }
        }

        if (advancePaymentBill.Status == BillingStatus.Confirming)
        {
            var billingId = advancePaymentBill.Id.ToString();
            _logger.LogInformation(
                "[BillingIndexerPollingWorker] Found confirming {0} bill {1}",
                advancePaymentBill.Type.ToString(), billingId);
            var billTransactionResult =
                await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null,
                    billingId, 0, 10);
            if (billTransactionResult == null || billTransactionResult.UserFundRecord == null ||
                billTransactionResult.UserFundRecord.Items == null ||
                billTransactionResult.UserFundRecord.Items.Count == 0)
            {
                return;
            }

            //Wait until approaching the safe height of LIB before processing
            var transactionResultDto = billTransactionResult.UserFundRecord.Items[0];
            var currentLatestBlockHeight = await _indexerProvider.GetCurrentVersionSyncBlockHeightAsync();
            if (currentLatestBlockHeight == 0)
            {
                _logger.LogError("[BillingIndexerPollingWorker]Get current latest block height failed");
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }

            //Update bill transaction id & status
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker]Get transaction {transactionResultDto.TransactionId} of billing {transactionResultDto.BillingId}");
            await _billingService.ConfirmPaymentAsync(advancePaymentBill.Id);
        }
    }

    private async Task HandleSettlementBillAsync(Guid organizationGuid,BillingDto settlementBill)
    {
        var organizationId = organizationGuid.ToString();
        if (settlementBill.Status == BillingStatus.Paid)
        {
            return;
        }

        if (settlementBill.Status == BillingStatus.Unpaid)
        {
            if (settlementBill.CreateTime.AddMinutes(_scheduledTaskOptions.UnpaidBillTimeoutMinutes) <
                DateTime.UtcNow)
            {
                //TODO Set bill status to failed
            }
        }

        if (settlementBill.Status == BillingStatus.Confirming)
        {
            var billingId = settlementBill.Id.ToString();
            _logger.LogInformation(
                "[BillingIndexerPollingWorker] Found confirming {0} bill {1}",
                settlementBill.Type.ToString(), billingId);
            var billTransactionResult =
                await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null, billingId,
                    0,
                    10);
            if (billTransactionResult == null || billTransactionResult.UserFundRecord == null ||
                billTransactionResult.UserFundRecord.Items == null ||
                billTransactionResult.UserFundRecord.Items.Count == 0)
            {
                return;
            }

            //Wait until approaching the safe height of LIB before processing
            var transactionResultDto = billTransactionResult.UserFundRecord.Items[0];
            var currentLatestBlockHeight = await _indexerProvider.GetCurrentVersionSyncBlockHeightAsync();
            if (currentLatestBlockHeight == 0)
            {
                _logger.LogError("[BillingIndexerPollingWorker]Get current latest block height failed");
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }

            //Update bill transaction id & status
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker]Get transaction {transactionResultDto.TransactionId} of billing {transactionResultDto.BillingId}");
            await _billingService.ConfirmPaymentAsync(settlementBill.Id);

            //Get organization wallet address
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                _logger.LogError($"Organization {organizationId} wallet address is null or empty, please check.");
                return;
            }

            //Create advance payment bill for current month
            var newAdvancePaymentBill = await _billingService.CreateAsync(organizationGuid,
                BillingType.AdvancePayment, DateTime.UtcNow);
            if (newAdvancePaymentBill == null)
            {
                _logger.LogWarning(
                    $"[BillingIndexerPollingWorker] create advance bill failed, the current month advance payment bill of organization {organizationId} is null");
                return;
            }

            //Check user organization balance
            var userOrganizationBalanceInfoDto =
                await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                    _contractOptions.BillingContractChainId, 0, 10);
            var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;

            if (organizationAccountBalance < newAdvancePaymentBill.PaidAmount)
            {
                //TODO Send email warning

                //Get organization asset
                var processorAssets = await _assetService.GetListsAsync(organizationGuid, new GetAssetInput()
                {
                    Type = MerchandiseType.Processor,
                    SkipCount = 0,
                    MaxResultCount = 50
                });

                //freeze app
                foreach (var assetDto in processorAssets.Items)
                {
                    var appId = assetDto.AppId;
                    if (appId != _graphQlOptions.BillingIndexerId)
                    {
                        await _appDeployService.FreezeAppAsync(appId);
                    }
                }

                return;
            }

            //Send lockFrom transaction to contract
            var sendLockFromTransactionOutput = await _billingContractProvider.BillingLockFromAsync(
                organizationWalletAddress, newAdvancePaymentBill.PaidAmount,
                newAdvancePaymentBill.Id.ToString());
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker] Send lock from transaction " +
                sendLockFromTransactionOutput.TransactionId +
                " of bill " + newAdvancePaymentBill.Id.ToString());
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
            _logger.LogInformation(
                $"After {lockFromResultQueryTimes} times retry, get lock from transaction {lockFromTransactionId} status {lockFromTransactionStatus}");
            if (lockFromTransactionStatus == TransactionState.Mined)
            {
                await _billingService.PayAsync(newAdvancePaymentBill.Id, lockFromTransactionId, DateTime.UtcNow);
                _logger.LogInformation($"Bill {newAdvancePaymentBill.Id.ToString()} is paying.");
            }
            else
            {
                _logger.LogWarning($"Bill {newAdvancePaymentBill.Id.ToString()} payment failed");
                //TODO Set bill status to failed

            }
        }
    }


    private async Task<List<BillingDto>> GetPaymentBillingListAsync(Guid organizationGuid,BillingType type)
    {
        var resultList = new List<BillingDto>();
        var now = DateTime.UtcNow;
        int skipCount = 0;
        int maxResultCount = 10;
        var billBeginTime = now.AddDays(-1);
        var billEndTime = now.AddDays(1);

        while (true)
        {
            var bills=await _billingService.GetListsAsync(organizationGuid, new GetBillingInput()
            {
                BeginTime = billBeginTime,
                EndTime = billEndTime,
                Type = type,
                SkipCount = skipCount,
                MaxResultCount = maxResultCount
            });
            if (bills?.Items == null || bills.Items.Count == 0)
            {
                break;
            }

            resultList.AddRange(bills.Items);

            if (bills.Items.Count < maxResultCount)
            {
                break;
            }

            skipCount += maxResultCount;
        }

        return resultList;
    }
    
    
}