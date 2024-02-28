using Orleans;

namespace AElfIndexer.Grains.Grain.Subscriptions;

public interface IAppCodeGrain: IGrainWithStringKey
{
    Task SetCodeAsync(byte[] code);
    Task<byte[]> GetCodeAsync();
    Task RemoveAsync();
}