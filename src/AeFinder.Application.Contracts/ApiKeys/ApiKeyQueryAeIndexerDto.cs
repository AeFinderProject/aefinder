using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryAeIndexerDto
{
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public string AppId { get; set; }
    public string AppName { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}