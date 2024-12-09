using System;

namespace AeFinder.ApiKeys;

public class ApiKeySummaryDto
{
    public Guid OrganizationId { get; set; }
    public int ApiKeyCount { get; set; }
    public int MaxApiKeyCount { get; set; }
    public long QueryLimit { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
    public long Query { get; set; }
}