using System;
using System.Linq;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using AElfIndexer.MongoDB;
using GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AElfIndexer.Dapp;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfIndexingElasticsearchModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfIndexerMongoDbModule),
    typeof(AElfIndexerClientModule))]
public class AElfIndexerDappBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureCors(context, configuration);
        ConfigureOrleans(context, configuration);
        ConfigureTokenCleanupService();
        context.Services.AddGraphQL(b => b
            .AddAutoClrMappings()
            .AddSystemTextJson()
            .AddErrorInfoProvider(e => e.ExposeExceptionDetails = true));

        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton<IClusterClient>(o => OrleansClusterClientFactory.GetClusterClient(configuration));
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async ()=> await client.Connect());
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseRouting();
        app.UseCors();
        app.UseConfiguredEndpoints();
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }
}