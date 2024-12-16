using System;
using Orleans;

namespace AeFinder.ApiKeys;

[GenerateSerializer]
public class ApiKeySummaryInfo
{
    [Id(0)]public Guid OrganizationId { get; set; }
    [Id(1)]public int ApiKeyCount { get; set; }
    [Id(2)]public long QueryLimit { get; set; }
    [Id(3)]public long TotalQuery { get; set; }
    [Id(4)]public DateTime LastQueryTime { get; set; }
}