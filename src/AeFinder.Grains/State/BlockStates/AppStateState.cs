namespace AeFinder.Grains.State.BlockStates;

public class AppStateState
{
    public AppState LastIrreversibleState { get; set; }
    
    public AppState PendingState { get; set; }
}
