using AeFinder.Apps;
using AeFinder.GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppCurrentVersionSetHandler : IDistributedEventHandler<AppCurrentVersionSetEto>, ITransientDependency
{
    private readonly ILogger<AppCurrentVersionSetHandler> _logger;
    private readonly IGraphQLAppService _graphQlAppService;

    public AppCurrentVersionSetHandler(IGraphQLAppService graphQlAppService, ILogger<AppCurrentVersionSetHandler> logger)
    {
        _graphQlAppService = graphQlAppService;
        _logger = logger;
    }

    public async Task HandleEventAsync(AppCurrentVersionSetEto eventData)
    {
        await _graphQlAppService.CacheAppCurrentVersionAsync(eventData.AppId, eventData.CurrentVersion);
        _logger.LogInformation("cache app current version, appId: {0}, currentVersion: {1}", eventData.AppId, eventData.CurrentVersion);
    }
}