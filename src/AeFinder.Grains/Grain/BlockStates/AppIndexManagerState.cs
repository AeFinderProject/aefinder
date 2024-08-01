namespace AeFinder.Grains.Grain.BlockStates;

[GenerateSerializer]
public class AppIndexManagerState
{
    [Id(0)] public List<string> IndexNameList { get; set; }
}