namespace AeFinder.Grains.State.BlockStates;

[GenerateSerializer]
public class AppStateState
{
    [Id(0)] public AppState LastIrreversibleState { get; set; }
    
    [Id(1)] public AppState PendingState { get; set; }
}
