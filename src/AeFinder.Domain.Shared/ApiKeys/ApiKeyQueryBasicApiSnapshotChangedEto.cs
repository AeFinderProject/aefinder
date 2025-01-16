using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiSnapshotChangedEto : QuerySnapshotEtoBase
{
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
}