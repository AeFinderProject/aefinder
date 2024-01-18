using AElfIndexer.Grains.State.BlockState;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockStates;

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