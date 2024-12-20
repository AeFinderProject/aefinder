namespace AeFinder.Options;

public class GraphQLOptions
{
    public string Configuration { get; set; }
    public string BillingIndexerSyncStateUrl { get; set; }
    public int SafeBlockCount { get; set; } = 100;
}