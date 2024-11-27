using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyInfo
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; }
    public ApiKeyStatus Status { get; set; }
    public bool IsEnableSpendingLimit { get; set; }
    public decimal SpendingLimitUsdt { get; set; }
    public HashSet<string> AuthorisedAeIndexers { get; set; } = new();
    public HashSet<string> AuthorisedDomains { get; set; } = new();
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}