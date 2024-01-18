using Orleans;

namespace AElfIndexer.Grains.Grain.BlockStates;

public interface IAppStateGrain : IGrainWithStringKey
{
    Task<string> GetLastIrreversibleStateAsync();
    Task SetLastIrreversibleStateAsync(string value);
}