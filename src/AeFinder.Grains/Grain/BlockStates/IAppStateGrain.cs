using AeFinder.Grains.State.BlockStates;
using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public interface IAppStateGrain : IGrainWithStringKey
{
    Task<AppState> GetLastIrreversibleStateAsync();
    Task SetLastIrreversibleStateAsync(AppState state);
    Task<AppStateDto> GetStateAsync();
    Task SetStateAsync(AppStateDto state);
}