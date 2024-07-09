using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using AeFinder.App.PlugIns;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams.Kafka.Utils;
using Volo.Abp.Modularity;

namespace AeFinder.App;

public class Startup
{
    private readonly IConfiguration _configuration;
    // private readonly IClusterClient _clusterClient;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
        // _clusterClient = clusterClient;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
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
            // var clusterClientFactory = services.GetRequiredService<IClusterClientFactory>();
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
        var appId = _configuration["AppInfo:AppId"];
        var version = _configuration["AppInfo:Version"];
        var apiServiceUrl = "";

        if (apiServiceUrl.IsNullOrEmpty())
        {
            throw new Exception("api service url config is missing!");
        }
        // var client = OrleansClusterClientFactory.GetClusterClient(_configuration);
        // await client.Connect();
        using (var httpClient = new HttpClient())
        {
            httpClient.BaseAddress = new Uri(apiServiceUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            // httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                string requestUrl = $"api/apps/code?appId={HttpUtility.UrlEncode(appId)}&version={HttpUtility.UrlEncode(version)}";
                HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                string base64EncodedData = await response.Content.ReadAsStringAsync();
                byte[] decodedBytes = Convert.FromBase64String(base64EncodedData);
                return decodedBytes;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                throw e;
            }
        }

        // var appSubscriptionGrain = clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        // return await appSubscriptionGrain.GetCodeAsync(version);
    }
}