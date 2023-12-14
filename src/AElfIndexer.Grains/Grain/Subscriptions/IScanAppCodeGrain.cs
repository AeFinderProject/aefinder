using Orleans;

namespace AElfIndexer.Grains.Grain.Subscriptions;

public interface IScanAppCodeGrain: IGrainWithStringKey
{
    Task SetCodeAsync(byte[] code);
    Task<byte[]> GetCodeAsync();
    Task RemoveAsync();
}