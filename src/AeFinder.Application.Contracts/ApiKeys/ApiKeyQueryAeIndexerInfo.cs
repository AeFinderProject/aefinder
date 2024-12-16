using System;
using Orleans;

namespace AeFinder.ApiKeys;

[GenerateSerializer]
public class ApiKeyQueryAeIndexerInfo
{
    [Id(0)]public Guid OrganizationId { get; set; }
    [Id(1)]public Guid ApiKeyId { get; set; }
    [Id(2)]public string AppId { get; set; }
    [Id(3)]public string AppName { get; set; }
    [Id(4)]public long TotalQuery { get; set; }
    [Id(5)]public DateTime LastQueryTime { get; set; }
}