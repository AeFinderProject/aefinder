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
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams.Kafka.Utils;
using Serilog;
using Volo.Abp.Modularity;

namespace AeFinder.App;

public class Startup
{
    private readonly IConfiguration _configuration;
    // private readonly IClusterClient _clusterClient;
    // private readonly ILogger<Startup> _logger;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
        // _clusterClient = clusterClient;
        // _logger = logger;
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
            Log.Information("already get app code"+ code.Length);
            // _logger.LogInformation("already get app code " + code.Length);
            options.PlugInSources.AddCode(code);
            Log.Information("add code complete");
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
        var apiServiceUrl = _configuration["ApiHostUrl"];

        if (apiServiceUrl.IsNullOrEmpty())
        {
            throw new Exception("api host url config is missing!");
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
                Log.Information("start request app code:" + requestUrl);
                HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Log.Information("response data length:" + responseBody.Length.ToString());
                string base64EncodedData = responseBody.Replace("\n", "").Replace("\r", "").Replace(" ", "").Trim('"');
                Log.Information("base64EncodedData data length:" + base64EncodedData.Length.ToString());
                byte[] decodedBytes = Convert.FromBase64String(base64EncodedData);
                Log.Information("decodedBytes data length:" + decodedBytes.Length.ToString());
                return decodedBytes;
            }
            catch (Exception e)
            {
                Log.Error($"Request app code error: {e.Message}");
                throw e;
            }
        }

        // var appSubscriptionGrain = clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        // return await appSubscriptionGrain.GetCodeAsync(version);
    }
}