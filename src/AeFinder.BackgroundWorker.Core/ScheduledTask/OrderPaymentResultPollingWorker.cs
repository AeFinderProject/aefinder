using AeFinder.BackgroundWorker.Options;
using AeFinder.Commons;
using AeFinder.Email;
using AeFinder.Options;
using AeFinder.Orders;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class OrderPaymentResultPollingWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<OrderPaymentResultPollingWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IUserAppService _userAppService;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IOrderService _orderService;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IBillingEmailSender _billingEmailSender;

    public OrderPaymentResultPollingWorker(AbpAsyncTimer timer,
        ILogger<OrderPaymentResultPollingWorker> logger, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationInformationProvider organizationInformationProvider,
        IUserAppService userAppService,
        IUserInformationProvider userInformationProvider,
        IOrganizationAppService organizationAppService,
        IOrderService orderService,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IBillingEmailSender billingEmailSender,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationInformationProvider = organizationInformationProvider;
        _userAppService = userAppService;
        _userInformationProvider = userInformationProvider;
        _organizationAppService = organizationAppService;
        _orderService = orderService;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _graphQlOptions = graphQlOptions.Value;
        _billingEmailSender = billingEmailSender;
        Timer.Period = _scheduledTaskOptions.OrderPaymentResultPollingTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessIndexerPollingAsync();
    }

    private async Task ProcessIndexerPollingAsync()
    {
        _logger.LogInformation("[OrderPaymentResultPollingWorker] Process indexer polling Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            var now = DateTime.UtcNow;
            
            //Check organization wallet address is bind
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                // _logger.LogWarning($"Organization {organizationId} wallet address is null or empty, please check.");
                continue;
            }
            
            //Handle payment orders
            var paymentOrders = await GetPaymentOrderListAsync(organizationUnitDto.Id);
            foreach (var paymentOrder in paymentOrders)
            {
                await HandlePaymentOrderAsync(organizationUnitDto.Id, paymentOrder);
            }
        }
    }
    
    private async Task HandlePaymentOrderAsync(Guid organizationGuid,OrderDto paymentOrder)
    {
        if (paymentOrder.Status == OrderStatus.Paid || paymentOrder.Status == OrderStatus.Canceled ||
            paymentOrder.Status == OrderStatus.PayFailed)
        {
            return;
        }

        //Automatically failed order that have remained unpaid for a long time
        if (paymentOrder.Status == OrderStatus.Unpaid)
        {
            if (paymentOrder.OrderTime.AddMinutes(_scheduledTaskOptions.UnpaidOrderTimeoutMinutes) <
                DateTime.UtcNow)
            {
                //Set order status to canceled
                _logger.LogInformation(
                    "[OrderPaymentResultPollingWorker] Cancel unpaid order {1}", paymentOrder.Id);
                await _orderService.CancelAsync(organizationGuid, paymentOrder.Id);
            }
        }

        if (paymentOrder.Status == OrderStatus.Confirming)
        {
            var orderId = paymentOrder.Id.ToString();
            _logger.LogInformation(
                "[OrderPaymentResultPollingWorker] Found confirming order {1}", orderId);
            var orderTransactionResult =
                await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null,
                    orderId, 0, 10);
            if (orderTransactionResult == null || orderTransactionResult.UserFundRecord == null ||
                orderTransactionResult.UserFundRecord.Items == null ||
                orderTransactionResult.UserFundRecord.Items.Count == 0)
            {
                return;
            }
            
            //Wait until approaching the safe height of LIB before processing
            var transactionResultDto = orderTransactionResult.UserFundRecord.Items[0];
            var currentLatestBlockHeight = await _indexerProvider.GetCurrentVersionSyncBlockHeightAsync();
            if (currentLatestBlockHeight == 0)
            {
                _logger.LogError("[OrderPaymentResultPollingWorker]Get current latest block height failed");
                return;
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }
            
            //Update bill transaction id & status
            _logger.LogInformation(
                $"[OrderPaymentResultPollingWorker]Get transaction {transactionResultDto.TransactionId} of order {transactionResultDto.BillingId}");
            await _orderService.ConfirmPaymentAsync(organizationGuid, paymentOrder.Id,
                transactionResultDto.TransactionId,
                transactionResultDto.Metadata.Block.BlockTime);
            
            //Send lock balance successful email
            var userInfo =
                await _userAppService.GetDefaultUserInOrganizationUnitAsync(paymentOrder.OrganizationId);
            await _billingEmailSender.SendLockBalanceSuccessfulNotificationAsync(userInfo.Email,
                transactionResultDto.Address, transactionResultDto.Amount,transactionResultDto.TransactionId
            );
        }
    }
    
    private async Task<List<OrderDto>> GetPaymentOrderListAsync(Guid organizationGuid)
    {
        var resultList = new List<OrderDto>();
        var now = DateTime.UtcNow;
        int skipCount = 0;
        int maxResultCount = 10;
        var orderBeginTime = now.AddDays(-1);
        var orderEndTime = now.AddDays(1);
        
        while (true)
        {
            var orders = await _orderService.GetListAsync(organizationGuid, new GetOrderListInput()
            {
                BeginTime = orderBeginTime,
                EndTime = orderEndTime,
                SkipCount = skipCount,
                MaxResultCount = maxResultCount
            });
            if (orders?.Items == null || orders.Items.Count == 0)
            {
                break;
            }

            resultList.AddRange(orders.Items);

            if (orders.Items.Count < maxResultCount)
            {
                break;
            }

            skipCount += maxResultCount;
        }
        
        return resultList;
    }
}