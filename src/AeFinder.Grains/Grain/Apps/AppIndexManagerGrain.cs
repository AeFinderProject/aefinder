using AeFinder.Apps;
using AeFinder.Grains.Grain.BlockStates;
using Microsoft.Extensions.Logging;

namespace AeFinder.Grains.Grain.Apps;

public class AppIndexManagerGrain : Grain<AppIndexManagerState>, IAppIndexManagerGrain
{
    private readonly IAppService _appService;
    private readonly ILogger<AppIndexManagerGrain> _logger;

    public AppIndexManagerGrain(IAppService appService, ILogger<AppIndexManagerGrain> logger)
    {
        _appService = appService;
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
        foreach (var indexName in State.IndexNameList)
        {
            await _appService.DeleteAppIndexAsync(indexName);
        }
    }
    
    public async Task ClearGrainStateAsync()
    {
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}