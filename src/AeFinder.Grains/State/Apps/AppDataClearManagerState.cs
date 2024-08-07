namespace AeFinder.Grains.State.Apps;

public class AppDataClearManagerState
{
    //Key: version Value: appId
    public Dictionary<string, string> VersionClearTasksDictionary { get; set; } = new ();
}