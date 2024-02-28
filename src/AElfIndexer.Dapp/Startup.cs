using System;
using System.Globalization;
using System.Threading.Tasks;
using AElfIndexer.Client;
using AElfIndexer.Client.PlugIns;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Apps;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams.Kafka.Utils;
using Volo.Abp.Modularity;

namespace AElfIndexer.Dapp;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        var clientType = _configuration["Client:ClientType"].ToEnum<ClientType>();
        switch (clientType)
        {
            case ClientType.Query:
                AddApplication<AElfIndexerDappQueryModule>(services);
                break;
            default:
                AddApplication<AElfIndexerDappModule>(services);
                break;
        }
    }

    private void AddApplication<T>(IServiceCollection services) where T : IAbpModule
    {
        services.AddApplicationAsync<T>(options =>
        {
            var code = AsyncHelper.RunSync(async () => await GetPluginCodeAsync());
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
    
    private async Task<byte[]> GetPluginCodeAsync()
    {
        var scanAppId = _configuration["AppInfo:ScanAppId"];
        var version = _configuration["AppInfo:Version"];
        
        var client = OrleansClusterClientFactory.GetClusterClient(_configuration);
        await client.Connect();
        var scanAppGrain = client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(scanAppId));
        return await scanAppGrain.GetCodeAsync(version);
    }
}