using AeFinder.ApiKeys;

namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeyQueryBasicApiSnapshotState : QuerySnapshotBaseState
{
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
}