using AeFinder.ApiKeys;

namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeyQueryBasicApiState
{
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public BasicApi Api { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}