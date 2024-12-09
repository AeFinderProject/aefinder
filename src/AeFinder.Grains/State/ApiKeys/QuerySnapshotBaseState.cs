using AeFinder.ApiKeys;

namespace AeFinder.Grains.State.ApiKeys;

public class QuerySnapshotBaseState
{
    public DateTime Time { get; set; }
    public long Query { get; set; }
    public Guid OrganizationId { get; set; }
    public SnapshotType Type { get; set; }
}