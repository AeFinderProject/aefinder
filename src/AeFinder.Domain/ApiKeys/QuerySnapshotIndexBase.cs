using System;
using Nest;

namespace AeFinder.ApiKeys;

public class QuerySnapshotIndexBase : AeFinderDomainEntity<string>
{
    public DateTime Time { get; set; }
    public long Query { get; set; }
    [Keyword]
    public Guid OrganizationId { get; set; }
    public SnapshotType Type { get; set; }
}