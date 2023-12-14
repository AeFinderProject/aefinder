using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.Subscriptions;

public class ScanAppCodeGrain : Grain<ScanAppCodeState>, IScanAppCodeGrain
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
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
    }
}