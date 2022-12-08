using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class DappDataGrain<T> : Grain<DappDataGrainState<T>>, IDappDataGrain<T>
{
    public Task<T> GetLatestValue()
    {
        return State.LatestValue != null ?
            Task.FromResult(State.LatestValue):
            Task.FromResult(State.LIBValue);
    }
    
    public Task<T> GetLIBValue()
    {
        return Task.FromResult(State.LIBValue);
    }

    public Task<DappDataValue<T>> GetValue()
    {
        return Task.FromResult(new DappDataValue<T>
        {
            LatestValue = State.LatestValue != null ? State.LatestValue : State.LIBValue,
            LIBValue = State.LIBValue
        });
    }

    public async Task SetLatestValue(T value)
    {
        State.LatestValue = value;
        await WriteStateAsync();
    }

    public async Task SetLIBValue(T value)
    {
        State.LIBValue = value;
        await WriteStateAsync();
    }
    
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        WriteStateAsync();
        return base.OnDeactivateAsync();
    }
}