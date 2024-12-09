namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeySnapshotState: QuerySnapshotBaseState
{
    public Guid ApiKeyId { get; set; }
}