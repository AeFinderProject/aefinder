using System;
using System.Collections.Generic;

namespace AeFinder.ApiKeys;

public class ApiKeyDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; }
    public string Key { get; set; }
    public ApiKeyStatus Status { get; set; }
    public bool IsEnableSpendingLimit { get; set; }
    public decimal SpendingLimitUsdt { get; set; }
    public List<AppInfoImmutable> AuthorisedAeIndexers { get; set; } = new();
    public HashSet<string> AuthorisedDomains { get; set; } = new();
    public HashSet<BasicApi> AuthorisedApis = new();
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}