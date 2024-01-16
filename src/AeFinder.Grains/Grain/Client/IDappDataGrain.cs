using Orleans;

namespace AeFinder.Grains.Grain.Client;

public interface IDappDataGrain : IGrainWithStringKey
{
    Task<string> GetLatestValue();
    Task<string> GetLIBValue();
    Task<DappDataValue> GetValue();
    Task SetLatestValue(string value);
    Task SetLIBValue(string value);
}