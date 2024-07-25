using AeFinder.Grains.State.Subscriptions;
using Orleans;

namespace AeFinder.Grains.Grain.Subscriptions;

public class AppCodeGrain : AeFinderGrain<AppCodeState>, IAppCodeGrain
{
    public async Task SetCodeAsync(byte[] code)
    {
        State.Code = code;
        await WriteStateAsync();
    }

    public async Task<byte[]> GetCodeAsync()
    {
        await ReadStateAsync();
        return State.Code;
    }

    public async Task RemoveAsync()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }
}