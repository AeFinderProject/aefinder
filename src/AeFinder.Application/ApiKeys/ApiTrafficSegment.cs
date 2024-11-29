using System;

namespace AeFinder.ApiKeys;

public class ApiTrafficSegmentBase
{
    public DateTime SegmentTime { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public long Query { get; set; }
    public DateTime LastQueryTime { get; set; }
}

public class AeIndexerApiTrafficSegment: ApiTrafficSegmentBase
{
    public string AppId { get; set; }
}

public class BasicDataApiTrafficSegment: ApiTrafficSegmentBase
{
    public BasicDataApiType BasicDataApiType { get; set; }
}