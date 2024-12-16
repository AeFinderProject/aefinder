using System;

namespace AeFinder.ApiKeys;

public class ApiKeySnapshotChangedEto: QuerySnapshotEtoBase
{
    public Guid ApiKeyId { get; set; }
}