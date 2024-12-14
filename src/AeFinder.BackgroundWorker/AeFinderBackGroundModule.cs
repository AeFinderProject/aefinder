using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Core;
using AeFinder.BackgroundWorker.Options;
using AeFinder.BackgroundWorker.ScheduledTask;
using AeFinder.Kubernetes;
using AeFinder.Kubernetes.Manager;
using AeFinder.Metrics;
using AeFinder.MongoDb;
using AElf.EntityMapping.Options;
using AElf.OpenTelemetry;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.CosmosDB;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AeFinderBackGroundCoreModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AeFinderKubernetesModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(OpenTelemetryModule))]
public class AeFinderBackGroundModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHttpClient();
        context.Services.AddHostedService<AeFinderHostedService>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderBackGroundModule>(); });
        context.Services.AddTransient<IAppDeployManager, KubernetesAppManager>();
        context.Services.AddTransient<IAppResourceLimitProvider, AppResourceLimitProvider>();
        context.Services.AddTransient<IKubernetesAppMonitor, KubernetesAppMonitor>();
        ConfigureHangfire(context, configuration);
        ConfigureTokenCleanupService();
        ConfigureEsIndexCreation();
        ConfigureCache(configuration);
        ConfigureMongoDbService(configuration, context);
        context.Services.Configure<ScheduledTaskOptions>(configuration.GetSection("ScheduledTask"));
        context.Services.Configure<TransactionRepairOptions>(configuration.GetSection("TransactionRepair"));
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }
    
    //Disable TokenCleanupBackgroundWorker
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AeFinder:"; });
    }

    private void ConfigureMongoDbService(IConfiguration configuration,ServiceConfigurationContext context)
    {
        // Register MongoDB Settings
        context.Services.Configure<OrleansDataClearOptions>(configuration.GetSection("OrleansDataClear"));
        // Register MongoClient
        context.Services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var mongoDbSettings = serviceProvider.GetRequiredService<IOptions<OrleansDataClearOptions>>().Value;
            return new MongoClient(mongoDbSettings.MongoDBClient);
        });
        context.Services.AddSingleton<IOrleansDbClearService, OrleansDbClearService>();
    }
    
    private void ConfigureEsIndexCreation()
    {
        Configure<CollectionCreateOptions>(x => { x.AddModule(typeof(AeFinderDomainModule)); });
    }
    
    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var mongoType = configuration["Hangfire:MongoType"];
        var connectionString = configuration["Hangfire:ConnectionString"];
        if(connectionString.IsNullOrEmpty()) return;

        if (mongoType.IsNullOrEmpty() ||
            mongoType.Equals(MongoType.MongoDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(x =>
            {
                x.UseMongoStorage(connectionString, new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    CheckConnection = true,
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
                });
            });
        }
        else if (mongoType.Equals(MongoType.DocumentDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(config =>
            {
                var mongoUrlBuilder = new MongoUrlBuilder(connectionString);
                var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
                var opt = new CosmosStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        BackupStrategy = new NoneMongoBackupStrategy(),
                        MigrationStrategy = new DropMongoMigrationStrategy(),
                    }
                };
                config.UseCosmosStorage(mongoClient, mongoUrlBuilder.DatabaseName, opt);
            });

            context.Services.AddHangfireServer(opt => { opt.Queues = new[] { "default", "notDefault" }; });
        }
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppDataClearWorker>());
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppRescanCheckWorker>());
        // AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppInfoSyncWorker>());
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppPodListSyncWorker>());
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppPodResourceInfoSyncWorker>());

        var transactionRepairOptions = context.ServiceProvider
            .GetRequiredService<IOptionsSnapshot<TransactionRepairOptions>>().Value;
        if (transactionRepairOptions.Enable)
        {
            AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<TransactionRepairWorker>());
        }
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {

    }
}