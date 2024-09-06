namespace AeFinder.BackgroundWorker.Options;

public class ScheduledTaskOptions
{
    public int AppInfoSyncTaskPeriodMilliSeconds { get; set; } = 86400000;
    public int AppRescanCheckTaskPeriodMilliSeconds { get; set; } = 600000;
}