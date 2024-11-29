using AeFinder.ApiKeys;

namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeyQueryBasicDataSnapshotState : QuerySnapshotBaseState
{
    public Guid ApiKeyId { get; set; }
    public BasicDataApiType BasicDataApiType { get; set; }
}