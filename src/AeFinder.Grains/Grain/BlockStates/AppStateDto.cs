using AeFinder.Grains.State.BlockStates;

namespace AeFinder.Grains.Grain.BlockStates;

public class AppStateDto
{
    public AppState LastIrreversibleState { get; set; }
    public AppState PendingState { get; set; }
}