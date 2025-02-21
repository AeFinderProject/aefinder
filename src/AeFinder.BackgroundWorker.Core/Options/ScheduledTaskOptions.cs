namespace AeFinder.BackgroundWorker.Options;

public class ScheduledTaskOptions
{
    public int AppInfoSyncTaskPeriodMilliSeconds { get; set; } = 86400000;
    public int AppRescanCheckTaskPeriodMilliSeconds { get; set; } = 600000;
    public int MaxAppRescanTimes { get; set; } = 3;
    public int AppPodListSyncTaskPeriodMilliSeconds { get; set; } = 600000;
    public int AppPodResourceSyncTaskPeriodMilliSeconds { get; set; } = 180000;
    public int MonthlyAutomaticChargeDay { get; set; } = 2;
    public int MonthlyAutomaticChargeTaskPeriodMilliSeconds { get; set; } = 3600000;
    public int BalanceWarningTaskPeriodMilliSeconds { get; set; } = 86400000;
    public int CleanExpiredAssetTaskPeriodMilliSeconds { get; set; } = 3600000;
    public int CheckPayFailedBillingTaskPeriodMilliSeconds { get; set; } = 3600000;
    public int RenewalAdvanceWarningDays { get; set; } = 5;
    public int OrganizationWalletSyncTaskPeriodMilliSeconds { get; set; } = 10000;
    public int OrderPaymentResultPollingTaskPeriodMilliSeconds { get; set; } = 12000;
    public int UnpaidOrderTimeoutMinutes { get; set; } = 30;
    public int UnpaidBillTimeOutDays { get; set; } = 7;
    public int PayFailedOrderTimeoutHours { get; set; } = 48;
    public int ConfirmingOrderTimeoutMinutes { get; set; } = 60;
    public int BillingPaymentTaskPeriodMilliSeconds { get; set; } = 300000;
    public int AppResourceUsageTaskPeriodMilliSeconds { get; set; } = 600000;
}