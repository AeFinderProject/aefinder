namespace AeFinder.BackgroundWorker.Options;

public class ScheduledTaskOptions
{
    public int AppInfoSyncTaskPeriodMilliSeconds { get; set; } = 86400000;
    public int AppRescanCheckTaskPeriodMilliSeconds { get; set; } = 600000;
    public int MaxAppRescanTimes { get; set; } = 3;
    public int AppPodListSyncTaskPeriodMilliSeconds { get; set; } = 600000;
    public int AppPodResourceSyncTaskPeriodMilliSeconds { get; set; } = 180000;
    public int RenewalBillMonth = 0;
    public int RenewalBillDay = 1;
    public int RenewalBillHour = 2;
    public int RenewalBillMinute = 0;
    public int BillingIndexerPollingTaskPeriodMilliSeconds { get; set; } = 10000;
}