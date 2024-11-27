namespace AeFinder.Grains.State.ApiKeys;

public class ApiKeySummaryState
{
    public Guid OrganizationId { get; set; }
    public long QueryLimit { get; set; }
    public long TotalQuery { get; set; }
    public DateTime LastQueryTime { get; set; }
}