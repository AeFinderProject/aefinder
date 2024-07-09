using System.Globalization;
using System.Threading.Tasks;
using AeFinder.App.PlugIns;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Streams.Kafka.Utils;
using Volo.Abp.Modularity;

namespace AeFinder.App;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IClusterClient _clusterClient;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
        // _clusterClient = clusterClient;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IClusterClient>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<IClusterClient>();
            return client;
        });
        var clientType = _configuration.GetValue("AppInfo:ClientType", ClientType.Full);
        switch (clientType)
        {
            case ClientType.Query:
                AddApplication<AeFinderAppHostQueryModule>(services);
                break;
            default:
                AddApplication<AeFinderAppHostModule>(services);
                break;
        }
    }

    private void AddApplication<T>(IServiceCollection services) where T : IAbpModule
    {
        services.AddApplicationAsync<T>(options =>
        {
            var orleansClient = services.GetRequiredService<IClusterClient>();
            var code = AsyncHelper.RunSync(async () => await GetPluginCodeAsync(orleansClient));
            options.PlugInSources.AddCode(code);
        });
    }
    
    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    // ReSharper disable once UnusedMember.Global
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var cultureInfo = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        
        app.UseCors();
        app.InitializeApplication();
        
            
    }
    
    private async Task<byte[]> GetPluginCodeAsync(IClusterClient clusterClient)
    {
        var appId = _configuration["AppInfo:AppId"];
        var version = _configuration["AppInfo:Version"];
        
        // var client = OrleansClusterClientFactory.GetClusterClient(_configuration);
        // await client.Connect();
        var appSubscriptionGrain = clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        return await appSubscriptionGrain.GetCodeAsync(version);
    }
}