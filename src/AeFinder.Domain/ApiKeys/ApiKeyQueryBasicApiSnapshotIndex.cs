using System;
using AElf.EntityMapping.Entities;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiSnapshotIndex : QuerySnapshotIndexBase, IEntityMappingEntity
{
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
}