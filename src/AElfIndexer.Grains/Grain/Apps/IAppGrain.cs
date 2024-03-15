using Orleans;

namespace AElfIndexer.Grains.Grain.Apps;

public interface IAppGrain : IGrainWithStringKey
{
    Task<ExistDto> Exists(string clientId);
}