using AeFinder.ApiKeys;

namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeyQueryBasicDataSnapshotState : QuerySnapshotBaseState
{
    public Guid ApiKeyId { get; set; }
    public BasicDataApi BasicDataApi { get; set; }
}