using System;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiIndex: AeFinderDomainEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public Guid OrganizationId { get; set; }
    [Keyword]
    public Guid ApiKeyId { get; set; }
    public int Api { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}