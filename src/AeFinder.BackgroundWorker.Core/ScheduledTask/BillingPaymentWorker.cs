using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class BillingPaymentWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IBillingService _billingService;
    private readonly IBillingManagementService _billingManagementService;

    public BillingPaymentWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions, IBillingManagementService billingManagementService,
        IBillingService billingService)
        : base(timer, serviceScopeFactory)
    {
        _billingManagementService = billingManagementService;
        _billingService = billingService;
        Timer.Period = scheduledTaskOptions.Value.BillingPaymentTaskPeriodMilliSeconds;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var outstandingBillings = await GetOutstandingBillingsAsync();
        foreach (var billing in outstandingBillings)
        {
            await _billingManagementService.PayAsync(billing.Id);
        }
    }

    private async Task<List<BillingDto>> GetOutstandingBillingsAsync()
    {
        var bills = await _billingService.GetListAsync(null, new GetBillingInput()
        {
            IsOutstanding= true,
            SkipCount = 0,
            MaxResultCount = 1000
        });

        return bills.Items.ToList();
    }
}