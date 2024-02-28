using Orleans;

namespace AElfIndexer.Grains.Grain.BlockState;

public interface IAppStateGrain : IGrainWithStringKey
{
    Task<string> GetLastIrreversibleStateAsync();
    Task SetLastIrreversibleStateAsync(string value);
}