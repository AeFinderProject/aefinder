namespace AeFinder.Grains.State.BlockStates;

[GenerateSerializer]
public class AppState
{
    [Id(0)] public string Type { get; set; }
    [Id(1)] public string Value { get; set; }
}