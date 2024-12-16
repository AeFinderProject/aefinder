using AeFinder.Apps;
using AeFinder.Commons;
using AeFinder.Grains.State.Apps;
using Microsoft.Extensions.Logging;

namespace AeFinder.Grains.Grain.Apps;

public class AppIndexManagerGrain : AeFinderGrain<AppIndexManagerState>, IAppIndexManagerGrain
{
    private readonly IElasticSearchIndexHelper _elasticSearchIndexHelper;
    private readonly ILogger<AppIndexManagerGrain> _logger;

    public AppIndexManagerGrain(IElasticSearchIndexHelper elasticSearchIndexHelper, ILogger<AppIndexManagerGrain> logger)
    {
        _elasticSearchIndexHelper = elasticSearchIndexHelper;
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
            await _elasticSearchIndexHelper.DeleteAppIndexAsync(indexName);
        }
    }
    
    public async Task ClearGrainStateAsync()
    {
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}