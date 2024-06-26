using AeFinder.App.BlockState;
using AeFinder.Grains.Grain.BlockStates;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AeFinder.App.Handlers;

public class LastIrreversibleBlockStateSetFoundHandler : ILocalEventHandler<LastIrreversibleBlockStateSetFoundEventData>,
    ITransientDependency
{
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppStateProvider _appStateProvider;
    private readonly IAppBlockStateChangeProvider _appBlockStateChangeProvider;
    private readonly ILogger<LastIrreversibleBlockStateSetFoundHandler> _logger;

    public LastIrreversibleBlockStateSetFoundHandler(IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppStateProvider appStateProvider, IAppBlockStateChangeProvider appBlockStateChangeProvider, ILogger<LastIrreversibleBlockStateSetFoundHandler> logger)
    {
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appStateProvider = appStateProvider;
        _appBlockStateChangeProvider = appBlockStateChangeProvider;
        _logger = logger;
    }

    public async Task HandleEventAsync(LastIrreversibleBlockStateSetFoundEventData eventData)
    {
        var toMergeBlockStateSets = new List<BlockStateSet>();
        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(eventData.ChainId, eventData.BlockHash);
        while (true)
        {
            if(blockStateSet != null)
            {
                toMergeBlockStateSets.Add(blockStateSet);
                blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(eventData.ChainId, blockStateSet.Block.PreviousBlockHash);
            }
            else
            {
                break;
            }
        }
        
        toMergeBlockStateSets = toMergeBlockStateSets.OrderBy(o => o.Block.BlockHeight).ToList();
        
        _logger.LogTrace("LastIrreversibleBlockStateSetFoundHandler PreMergeStateAsync");
        await _appStateProvider.PreMergeStateAsync(eventData.ChainId, toMergeBlockStateSets);

        _logger.LogTrace("LastIrreversibleBlockStateSetFoundHandler SetLastIrreversibleBlockStateSetAsync");
        await _appBlockStateSetProvider.SetLastIrreversibleBlockStateSetAsync(eventData.ChainId, eventData.BlockHash);
        await _appBlockStateSetProvider.SaveDataAsync(eventData.ChainId);
        
        _logger.LogTrace("LastIrreversibleBlockStateSetFoundHandler CleanBlockStateChangeAsync");
        await _appBlockStateChangeProvider.CleanBlockStateChangeAsync(eventData.ChainId, eventData.BlockHeight);

        _logger.LogTrace("LastIrreversibleBlockStateSetFoundHandler MergeStateAsync");
        await _appStateProvider.MergeStateAsync(eventData.ChainId, toMergeBlockStateSets);
        
        _appBlockStateSetProvider.CleanBlockStateSets(eventData.ChainId, eventData.BlockHeight,
            eventData.BlockHash);
    }
}