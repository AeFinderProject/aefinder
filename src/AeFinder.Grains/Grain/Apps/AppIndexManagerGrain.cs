using AeFinder.Apps;
using AeFinder.Commons;
using AeFinder.Grains.State.Apps;
using Microsoft.Extensions.Logging;

namespace AeFinder.Grains.Grain.Apps;

public class AppIndexManagerGrain : AeFinderGrain<AppIndexManagerState>, IAppIndexManagerGrain
{
    private readonly IElasticSearchCommonService _elasticSearchCommonService;
    private readonly ILogger<AppIndexManagerGrain> _logger;

    public AppIndexManagerGrain(IElasticSearchCommonService elasticSearchCommonService, ILogger<AppIndexManagerGrain> logger)
    {
        _elasticSearchCommonService = elasticSearchCommonService;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public async Task AddIndexNameAsync(string indexName)
    {
        if (State.IndexNameList != null && State.IndexNameList.Contains(indexName))
        {
            return;
        }

        if (State.IndexNameList == null)
        {
            State.IndexNameList = new List<string>();
        }
        State.IndexNameList.Add(indexName);

        await WriteStateAsync();
    }

    public async Task ClearVersionIndexAsync()
    {
        if (State.IndexNameList == null)
        {
            return;
        }
        foreach (var indexName in State.IndexNameList)
        {
            await _elasticSearchCommonService.DeleteAppIndexAsync(indexName);
        }
    }
    
    public async Task ClearGrainStateAsync()
    {
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}