using System;
using System.Linq;
using AElf.Client;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfIndexer.Dapp;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfIndexingElasticsearchModule),
    typeof(AbpAspNetCoreSerilogModule))]
public class AElfIndexerDappModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // context.Services.AddSingleton<IClusterClient>(o =>
        // {
            // return new ClientBuilder()
            //     .ConfigureDefaults()
            //     .UseRedisClustering(opt =>
            //     {
            //         opt.ConnectionString = configuration["Orleans:ClusterDbConnection"];
            //         opt.Database = Convert.ToInt32(configuration["Orleans:ClusterDbNumber"]);
            //     })
            //     .Configure<ClusterOptions>(options =>
            //     {
            //         options.ClusterId = configuration["Orleans:ClusterId"];
            //         options.ServiceId = configuration["Orleans:ServiceId"];
            //     })
            //     .ConfigureApplicationParts(parts =>
            //         parts.AddApplicationPart(typeof(AElfIndexerGrainsModule).Assembly).WithReferences())
            //     .AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
            //     .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
            //     .Build();
        // });
        ConfigureCors(context, configuration);
        context.Services.AddGraphQL(b => b
            .AddAutoClrMappings()
            .AddSystemTextJson()
            .AddErrorInfoProvider(e => e.ExposeExceptionDetails = true));
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

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        // AsyncHelper.RunSync(async ()=> await client.Connect());
        var app = context.GetApplicationBuilder();
        app.UseRouting();
        app.UseCors();
        app.UseConfiguredEndpoints();
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        // var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        // AsyncHelper.RunSync(client.Close);
    }
}