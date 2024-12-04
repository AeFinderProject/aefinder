using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryApiDto
{
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}