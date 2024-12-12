namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyOptions
{
    public int FlushPeriod { get; set; } = 5; // 5 minutes
    public int MaxApiKeyCount { get; set; } = 10;
    public HashSet<string> IgnoreKeys { get; set; } = new();
}