using AeFinder.Grains.State.BlockStates;
using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public class AppStateGrain : Grain<AppStateState>, IAppStateGrain
{
    public async Task<string> GetLastIrreversibleStateAsync()
    {
        await ReadStateAsync();
        return State.LastIrreversibleState;
    }

    public async Task SetLastIrreversibleStateAsync(string value)
    {
        State.LastIrreversibleState = value;
        await WriteStateAsync();
    }
}