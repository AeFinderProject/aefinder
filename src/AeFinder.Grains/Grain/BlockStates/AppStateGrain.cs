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

    public async Task<AppStateDto> GetStateAsync()
    {
        await ReadStateAsync();
        return new AppStateDto
        {
            LastIrreversibleState = State.LastIrreversibleState,
            PendingState = State.PendingState
        };
    }

    public async Task SetStateAsync(AppStateDto state)
    {
        State.LastIrreversibleState = state.LastIrreversibleState;
        State.PendingState = state.PendingState;
        await WriteStateAsync();
    }
}