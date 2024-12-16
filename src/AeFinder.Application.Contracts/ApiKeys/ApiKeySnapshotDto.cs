using System;

namespace AeFinder.ApiKeys;

public class ApiKeySnapshotDto : QuerySnapshotDtoBase
{
    public Guid ApiKeyId { get; set; }
}