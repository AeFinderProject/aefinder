using System;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.ApiKeys;

public class ApiKeySnapshotIndex: QuerySnapshotIndexBase, IEntityMappingEntity
{
    [Keyword]
    public Guid ApiKeyId { get; set; }
}