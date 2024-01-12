namespace AElfIndexer.Grains.State.ScanApps;

public class ScanAppState
{
    public ScanAppVersion CurrentVersion { get;set; }
    public ScanAppVersion NewVersion { get; set; }
}

public class ScanAppVersion
{
    public string Version { get; set; }
    public SubscriptionStatus Status { get; set; }
}