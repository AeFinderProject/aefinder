using System;

namespace AeFinder.ApiKeys;

public class ApiKeySummaryChangedEto
{
    public string Id { get; set; }
    public Guid OrganizationId { get; set; }
    public int ApiKeyCount { get; set; }
    public long QueryLimit { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}