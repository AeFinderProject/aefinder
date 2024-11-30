using System;
using Orleans;

namespace AeFinder.ApiKeys;

[GenerateSerializer]
public class ApiKeySummaryInfo
{
    public Guid OrganizationId { get; set; }
    public long QueryLimit { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}