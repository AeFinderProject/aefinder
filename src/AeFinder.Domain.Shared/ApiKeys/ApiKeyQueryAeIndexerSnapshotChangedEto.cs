using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryAeIndexerSnapshotChangedEto: QuerySnapshotEtoBase
{
    public Guid ApiKeyId { get; set; }
    public string AppId { get; set; }
    public string AppName { get; set; }
}