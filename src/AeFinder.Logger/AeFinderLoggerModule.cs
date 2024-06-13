using System.Text;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.Modularity;

namespace AeFinder.Logger;

public class AeFinderLoggerModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        // Read the node list from the configuration file
        Configure<LogElasticSearchOptions>(configuration.GetSection("LogElasticSearch"));
        context.Services.AddSingleton<ElasticClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<LogElasticSearchOptions>>().Value;
            if (options == null)
            {
                throw new Exception("the config of [LogElasticSearch] is missing.");
            }
            var nodes = options.Uris.Select(uri => new Uri(uri)).ToArray();
            var connectionPool = new StaticConnectionPool(nodes);
            var settings = new ConnectionSettings(connectionPool)
                .EnableHttpCompression() // Enable HTTP compression
                // .PrettyJson();            // Set the requested JSON to formatted output
                .OnRequestCompleted(callDetails =>
                {
                    // Displays the request and response details
                    Console.WriteLine(
                        $"Method: {callDetails.HttpMethod} Path: {callDetails.Uri} Status: {callDetails.HttpStatusCode}");
                    if (callDetails.RequestBodyInBytes != null)
                    {
                        Console.WriteLine($"Request: {Encoding.UTF8.GetString(callDetails.RequestBodyInBytes)}");
                    }

                    if (callDetails.ResponseBodyInBytes != null)
                    {
                        Console.WriteLine($"Response: {Encoding.UTF8.GetString(callDetails.ResponseBodyInBytes)}");
                    }
                });
            return new ElasticClient(settings);
        });
        
        context.Services.AddSingleton(typeof(ILogService), typeof(LogElasticSearchService));
    }
}