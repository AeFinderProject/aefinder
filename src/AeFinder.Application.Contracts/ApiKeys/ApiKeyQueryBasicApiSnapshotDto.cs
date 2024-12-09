using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiSnapshotDto : QuerySnapshotDtoBase
{
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
}