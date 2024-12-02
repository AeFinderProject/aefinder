using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiSnapshotEto : QuerySnapshotEtoBase
{
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
}