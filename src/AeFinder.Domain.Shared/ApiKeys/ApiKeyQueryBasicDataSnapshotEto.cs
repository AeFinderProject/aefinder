using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicDataSnapshotEto : QuerySnapshotEtoBase
{
    public Guid ApiKeyId { get; set; }
    public BasicDataApiType BasicDataApiType { get; set; }
}