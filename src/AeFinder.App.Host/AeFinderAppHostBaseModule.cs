using System;
using System.Linq;
using AeFinder.App.Deploy;
using AeFinder.App.Metrics;
using AeFinder.Apps;
using AeFinder.MongoDb;
using AElf.OpenTelemetry;
using GraphQL;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Orleans;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AeFinder.App;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AeFinderApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderAppModule),
    typeof(OpenTelemetryModule))]
public class AeFinderAppHostBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureCors(context, configuration);
        ConfigureTokenCleanupService();
        context.Services.AddGraphQL(b => b
            .AddAutoClrMappings()
            .AddSystemTextJson()
            .AddErrorInfoProvider(e => e.ExposeExceptionDetails = true));

        context.Services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder
                    .AddMeter("AeFinder.App.Host");
                builder.AddPrometheusExporter();
            });
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
        context.Services.AddTransient<IAppResourceLimitProvider, AppResourceLimitProvider>();
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
                    .AllowAnyMethod();
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

    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var appInfoOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        var graphqlPath = $"/{appInfoOptions.AppId}/{appInfoOptions.Version}/graphql";
        
        
        app.UseGraphQLHttpMetrics(graphqlPath);
        app.UseGraphQL(graphqlPath);
        app.UseGraphQLPlayground(
            $"/{appInfoOptions.AppId}/{appInfoOptions.Version}/ui/playground",
            new PlaygroundOptions
            {
                GraphQLEndPoint = "../graphql",
                SubscriptionsEndPoint = "../graphql",
            });
        app.UseGraphQLGraphiQL(
            $"/{appInfoOptions.AppId}/{appInfoOptions.Version}/ui/graphiql",
            new GraphiQLOptions()
            {
                GraphQLEndPoint = "../graphql",
                SubscriptionsEndPoint = "../graphql",
            }
        );
        app.UseOpenTelemetryPrometheusScrapingEndpoint($"/{appInfoOptions.AppId}/{appInfoOptions.Version}/metrics");
        app.UseRouting();
        app.UseCors();
        app.UseConfiguredEndpoints();
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {

    }
}