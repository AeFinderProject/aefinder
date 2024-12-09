using System;

namespace AeFinder.ApiKeys;

public class GetSnapshotInput
{
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SnapshotType Type { get; set; }
}