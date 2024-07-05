using AeFinder.Grains.State.BlockStates;

namespace AeFinder.Grains.Grain.BlockStates;

[GenerateSerializer]
public class AppStateDto
{
    [Id(0)]public AppState LastIrreversibleState { get; set; }
    [Id(1)]public AppState PendingState { get; set; }
}