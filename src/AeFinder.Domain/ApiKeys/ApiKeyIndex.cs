using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.ApiKeys;

public class ApiKeyIndex: AeFinderDomainEntity<Guid>, IEntityMappingEntity
{
    public Guid OrganizationId { get; set; }
    [Keyword]
    public string Name { get; set; }
    [Keyword]
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