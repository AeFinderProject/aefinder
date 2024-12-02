using System;

namespace AeFinder.ApiKeys;

public class ApiKeySnapshotEto: QuerySnapshotEtoBase
{
    public Guid ApiKeyId { get; set; }
}