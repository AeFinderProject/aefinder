using System;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryAeIndexerSnapshotIndex: QuerySnapshotIndexBase, IEntityMappingEntity
{
    [Keyword]
    public Guid ApiKeyId { get; set; }
    [Keyword]
    public string AppId { get; set; }
    [Keyword]
    public string AppName { get; set; }
}