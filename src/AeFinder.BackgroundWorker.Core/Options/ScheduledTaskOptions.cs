namespace AeFinder.BackgroundWorker.Options;

public class ScheduledTaskOptions
{
    public int AppInfoSyncTaskPeriodMilliSeconds { get; set; } = 86400000;
    public int AppRescanCheckTaskPeriodMilliSeconds { get; set; } = 600000;
    public int MaxAppRescanTimes { get; set; } = 3;
    public int AppPodListSyncTaskPeriodMilliSeconds { get; set; } = 600000;
    public int AppPodResourceSyncTaskPeriodMilliSeconds { get; set; } = 180000;
    public int MonthlyAutomaticChargeDay { get; set; } = 2;
    public int MonthlyAutomaticChargeTaskPeriodMilliSeconds { get; set; } = 86400000;
}