using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using AeFinder.App.PlugIns;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams.Kafka.Utils;
using Serilog;
using Volo.Abp.Modularity;

namespace AeFinder.App;

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
        var apiServiceUrl = _configuration["ApiHostUrl"];

        if (apiServiceUrl.IsNullOrEmpty())
        {
            throw new Exception("api host url config is missing!");
        }

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

                string responseBody = await response.Content.ReadAsStringAsync();
                string base64EncodedData = responseBody.Trim('"');
                byte[] decodedBytes = Convert.FromBase64String(base64EncodedData);
                return decodedBytes;
            }
            catch (Exception e)
            {
                Log.Error($"Request app code error: {e.Message}");
                throw e;
            }
        }
    }
}