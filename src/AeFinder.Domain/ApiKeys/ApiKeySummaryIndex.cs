using System;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.ApiKeys;

public class ApiKeySummaryIndex: AeFinderDomainEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public Guid OrganizationId { get; set; }
    public int ApiKeyCount { get; set; }
    public long QueryLimit { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}