using System;
using AElf.EntityMapping.Entities;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicDataSnapshotIndex : QuerySnapshotIndexBase, IEntityMappingEntity
{
    public Guid ApiKeyId { get; set; }
    public BasicDataApiType BasicDataApiType { get; set; }
}