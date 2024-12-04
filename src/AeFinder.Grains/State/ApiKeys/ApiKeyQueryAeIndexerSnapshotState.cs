namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeyQueryAeIndexerSnapshotState : QuerySnapshotBaseState
{
    public Guid ApiKeyId { get; set; }
    public string AppId { get; set; }
    public string AppName { get; set; }
}