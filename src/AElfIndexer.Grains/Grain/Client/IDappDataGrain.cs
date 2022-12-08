using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IDappDataGrain<T> : IGrainWithStringKey
{
    Task<T> GetLatestValue();
    Task<T> GetLIBValue();
    Task<DappDataValue<T>> GetValue();
    Task SetLatestValue(T value);
    Task SetLIBValue(T value);
}