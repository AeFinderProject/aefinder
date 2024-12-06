using System;
using System.Collections.Generic;
using AeFinder.Apps;
using Orleans;

namespace AeFinder.ApiKeys;

[GenerateSerializer]
public class ApiKeyInfo
{
    [Id(0)]public Guid Id { get; set; }
    [Id(1)]public Guid OrganizationId { get; set; }
    [Id(2)]public string Name { get; set; }
    [Id(3)]public string Key { get; set; }
    [Id(4)]public bool IsEnableSpendingLimit { get; set; }
    [Id(5)]public decimal SpendingLimitUsdt { get; set; }
    [Id(6)]public Dictionary<string, AppInfoImmutable> AuthorisedAeIndexers { get; set; } = new();
    [Id(7)]public HashSet<string> AuthorisedDomains { get; set; } = new();
    [Id(8)]public HashSet<BasicApi> AuthorisedApis = new();
    [Id(9)]public long TotalQuery { get; set; }
    [Id(10)]public DateTime LastQueryTime { get; set; }
    [Id(11)]public bool IsDeleted { get; set; }
    [Id(12)]public DateTime CreateTime { get; set; }
    [Id(13)]public DateTime UpdateTime { get; set; }
}