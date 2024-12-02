using System;
using AElf.EntityMapping.Entities;

namespace AeFinder.ApiKeys;

public class ApiKeySnapshotIndex: QuerySnapshotIndexBase, IEntityMappingEntity
{
    public Guid ApiKeyId { get; set; }
}