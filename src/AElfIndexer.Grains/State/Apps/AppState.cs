namespace AElfIndexer.Grains.State.Apps;

public class AppState
{
    public AppVersion CurrentVersion { get;set; }
    public AppVersion NewVersion { get; set; }
}

public class AppVersion
{
    public string Version { get; set; }
    public SubscriptionStatus Status { get; set; }
}