using System;
using AElf.EntityMapping.Entities;

namespace AeFinder.ApiKeys;

public class ApiKeySummaryIndex: AeFinderDomainEntity<string>, IEntityMappingEntity
{
    public Guid OrganizationId { get; set; }
    public long QueryLimit { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}