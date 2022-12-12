using Newtonsoft.Json;

namespace AElfIndexer.Grains.Grain.Client;

public static class IDappDataGrainExtensions
{
    public static async Task<T> GetLatestValue<T>(this IDappDataGrain grain)
    {
        var latestValue = await grain.GetLatestValue();
        return latestValue != null ? JsonConvert.DeserializeObject<T>(latestValue) : default;
    }
    
    public static async Task<T> GetLIBValue<T>(this IDappDataGrain grain)
    {
        var libValue = await grain.GetLIBValue();
        return libValue != null ? JsonConvert.DeserializeObject<T>(await grain.GetLIBValue()) : default;
    }
    
    public static async Task<DappDataValue<T>> GetValue<T>(this IDappDataGrain grain)
    {
        var dataValue = await grain.GetValue();
        return new DappDataValue<T>
        {
            LatestValue = dataValue.LatestValue != null
                ? JsonConvert.DeserializeObject<T>(dataValue.LatestValue)
                : default,
            LIBValue = dataValue.LIBValue != null ? JsonConvert.DeserializeObject<T>(dataValue.LIBValue) : default
        };
    }
    
    public static async Task SetLatestValue<T>(this IDappDataGrain grain, T value)
    {
        await grain.SetLatestValue(JsonConvert.SerializeObject(value));
    }
    
    public static async Task SetLIBValue<T>(this IDappDataGrain grain, T value)
    {
        await grain.SetLIBValue(JsonConvert.SerializeObject(value));
    }
}