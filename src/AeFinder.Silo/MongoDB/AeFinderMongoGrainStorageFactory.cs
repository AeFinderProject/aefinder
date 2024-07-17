using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Storage;

namespace AeFinder.Silo.MongoDB;

public static class AeFinderMongoGrainStorageFactory
{
    public static IGrainStorage Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>();
        return ActivatorUtilities.CreateInstance<AeFinderMongoGrainStorage>(services, optionsMonitor.Get(name));
    }
}