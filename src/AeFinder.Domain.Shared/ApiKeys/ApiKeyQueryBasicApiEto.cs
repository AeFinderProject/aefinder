using System;

namespace AeFinder.ApiKeys;

public class ApiKeyQueryBasicApiEto
{
    public string Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}