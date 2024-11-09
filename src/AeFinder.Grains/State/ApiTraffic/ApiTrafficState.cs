namespace AeFinder.Grains.State.ApiTraffic;

[GenerateSerializer]
public class ApiTrafficState
{
    [Id(0)] public long RequestCount { get; set; }
}