using AeFinder.ApiKeys;

namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeyQueryBasicDataState
{
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public BasicDataApi BasicDataApi { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}