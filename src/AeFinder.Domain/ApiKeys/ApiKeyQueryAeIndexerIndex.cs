using System;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryAeIndexerIndex: AeFinderDomainEntity<string>, IEntityMappingEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    [Keyword]
    public string AppId { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}