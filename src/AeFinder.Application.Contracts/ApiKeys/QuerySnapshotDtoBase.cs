using System;

namespace AeFinder.ApiKeys;

public class QuerySnapshotDtoBase
{
    public DateTime Time { get; set; }
    public long Query { get; set; }
    public Guid OrganizationId { get; set; }
    public SnapshotType Type { get; set; }
}