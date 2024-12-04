using System;
using AElf.EntityMapping.Entities;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryAeIndexerSnapshotIndex: QuerySnapshotIndexBase, IEntityMappingEntity
{
    public Guid ApiKeyId { get; set; }
    public string AppId { get; set; }
    public string AppName { get; set; }
}