using System;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiSnapshotIndex : QuerySnapshotIndexBase, IEntityMappingEntity
{
    [Keyword]
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
}