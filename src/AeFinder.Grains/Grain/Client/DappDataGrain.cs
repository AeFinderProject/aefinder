using AeFinder.Grains.State.Client;
using Orleans;

namespace AeFinder.Grains.Grain.Client;

public class DappDataGrain : Grain<DappDataGrainState>, IDappDataGrain
{
    public Task<string> GetLatestValue()
    {
        return State.LatestValue != null ?
            Task.FromResult(State.LatestValue):
            Task.FromResult(State.LIBValue);
    }
    
    public Task<string> GetLIBValue()
    {
        return Task.FromResult(State.LIBValue);
    }

    public Task<DappDataValue> GetValue()
    {
        return Task.FromResult(new DappDataValue
        {
            LatestValue = State.LatestValue != null ? State.LatestValue : State.LIBValue,
            LIBValue = State.LIBValue
        });
    }

    public async Task SetLatestValue(string value)
    {
        State.LatestValue = value;
        await WriteStateAsync();
    }

    public async Task SetLIBValue(string value)
    {
        State.LIBValue = value;
        await WriteStateAsync();
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
}