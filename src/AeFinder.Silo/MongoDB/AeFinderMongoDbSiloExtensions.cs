using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Runtime.Hosting;

namespace AeFinder.Silo.MongoDB;

public static class AeFinderMongoDbSiloExtensions
{
    public static ISiloBuilder AddAeFinderMongoDBGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<MongoDBGrainStorageOptions> configureOptions)
    {
        return builder.ConfigureServices((Action<IServiceCollection>)(services =>
            services.AddAeFinderMongoDBGrainStorage(name, configureOptions)));
    }

    public static IServiceCollection AddAeFinderMongoDBGrainStorage(
        this IServiceCollection services,
        string name,
        Action<MongoDBGrainStorageOptions> configureOptions)
    {
        return services.AddAeFinderMongoDBGrainStorage(name, ob => ob.Configure(configureOptions));
    }

    public static IServiceCollection AddAeFinderMongoDBGrainStorage(this IServiceCollection services, string name,
        Action<OptionsBuilder<MongoDBGrainStorageOptions>> configureOptions = null)
    {
        configureOptions?.Invoke(services.AddOptions<MongoDBGrainStorageOptions>(name));
        services.TryAddSingleton<IGrainStateSerializer, JsonGrainStateSerializer>();
        services.AddTransient<IConfigurationValidator>(sp =>
            new MongoDBGrainStorageOptionsValidator(
                sp.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>().Get(name), name));
        services.ConfigureNamedOptionForLogging<MongoDBGrainStorageOptions>(name);
        services.AddTransient<IPostConfigureOptions<MongoDBGrainStorageOptions>, AeFinderMongoDBGrainStorageConfigurator>();
        return services.AddGrainStorage(name, AeFinderMongoGrainStorageFactory.Create);
    }
}
