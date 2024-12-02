using System;
using AElf.EntityMapping.Entities;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiIndex: AeFinderDomainEntity<string>, IEntityMappingEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}