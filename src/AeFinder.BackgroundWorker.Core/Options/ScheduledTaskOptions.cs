namespace AeFinder.BackgroundWorker.Options;

public class ScheduledTaskOptions
{
    public int AppInfoSyncTaskPeriodMilliSeconds { get; set; } = 86400000;
    public int AppRescanCheckTaskPeriodMilliSeconds { get; set; } = 600000;
    public int MaxAppRescanTimes { get; set; } = 3;
    public int AppPodListSyncTaskPeriodMilliSeconds { get; set; } = 600000;
    public int AppPodResourceSyncTaskPeriodMilliSeconds { get; set; } = 180000;
    public int RenewalBillMonth { get; set; }  = 0;
    public int RenewalBillDay { get; set; }  = 1;
    public int RenewalBillHour { get; set; }  = 2;
    public int RenewalBillMinute { get; set; }  = 0;
    public int BillingIndexerPollingTaskPeriodMilliSeconds { get; set; } = 10000;
    public int RenewalBalanceCheckTaskPeriodMilliSeconds { get; set; } = 86400000;
}