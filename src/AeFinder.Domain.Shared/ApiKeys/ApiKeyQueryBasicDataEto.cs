using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicDataEto
{
    public string Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public BasicDataApiType BasicDataApiType { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}