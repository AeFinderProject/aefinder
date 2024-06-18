using AeFinder.App.BlockState;
using AeFinder.Grains.Grain.BlockStates;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AeFinder.App.Handlers;

public class LastIrreversibleBlockStateSetFoundHandler : ILocalEventHandler<LastIrreversibleBlockStateSetFoundEventData>,
    ITransientDependency
{
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppStateProvider _appStateProvider;
    private readonly IAppBlockStateChangeProvider _appBlockStateChangeProvider;

    public LastIrreversibleBlockStateSetFoundHandler(IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppStateProvider appStateProvider, IAppBlockStateChangeProvider appBlockStateChangeProvider)
    {
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appStateProvider = appStateProvider;
        _appBlockStateChangeProvider = appBlockStateChangeProvider;
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
        
        await _appStateProvider.PreMergeStateAsync(eventData.ChainId, toMergeBlockStateSets);

        await _appBlockStateSetProvider.SetLastIrreversibleBlockStateSetAsync(eventData.ChainId, eventData.BlockHash);
        await _appBlockStateSetProvider.SaveDataAsync(eventData.ChainId);
        
        await _appBlockStateChangeProvider.CleanBlockStateChangeAsync(eventData.ChainId, eventData.BlockHeight);

        await _appStateProvider.MergeStateAsync(eventData.ChainId, toMergeBlockStateSets);
        
        _appBlockStateSetProvider.CleanBlockStateSets(eventData.ChainId, eventData.BlockHeight,
            eventData.BlockHash);
    }
}