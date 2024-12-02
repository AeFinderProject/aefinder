using System;

namespace AeFinder.ApiKeys;

public class ApiKeySummaryEto
{
    public string Id { get; set; }
    public Guid OrganizationId { get; set; }
    public long QueryLimit { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}