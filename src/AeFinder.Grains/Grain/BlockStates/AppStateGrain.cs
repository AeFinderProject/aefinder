using AeFinder.Grains.State.BlockStates;
using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public class AppStateGrain : Grain<AppStateState>, IAppStateGrain
{
    public async Task<AppState> GetLastIrreversibleStateAsync()
    {
        await ReadStateAsync();
        return State.LastIrreversibleState;
    }

    public async Task SetLastIrreversibleStateAsync(AppState state)
    {
        State.LastIrreversibleState = state;
        await WriteStateAsync();
    }
}