namespace AeFinder.Grains.State.Apps;

[GenerateSerializer]
public class AppIndexManagerState
{
    [Id(0)] public List<string> IndexNameList { get; set; } = new();
}