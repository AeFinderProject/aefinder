using AElfIndexer.Client.BlockState;
using AElfIndexer.Grains.Grain.BlockStates;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElfIndexer.Client.Handlers;

public class LastIrreversibleBlockStateSetFoundHandler : ILocalEventHandler<LastIrreversibleBlockStateSetFoundEventData>,
    ITransientDependency
{
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppStateProvider _appStateProvider;

    public LastIrreversibleBlockStateSetFoundHandler(IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppStateProvider appStateProvider)
    {
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appStateProvider = appStateProvider;
    }

    public async Task HandleEventAsync(LastIrreversibleBlockStateSetFoundEventData eventData)
    {
        
        var blockStateSets = await _appBlockStateSetProvider.GetBlockStateSetsAsync(eventData.ChainId);
        var toMergeBlockStateSets = new List<BlockStateSet>();
        var blockHash = eventData.BlockHash;
        while (true)
        {
            if(blockStateSets.TryGetValue(blockHash, out var blockStateSet))
            {
                toMergeBlockStateSets.Add(blockStateSet);
                blockHash = blockStateSet.Block.PreviousBlockHash;
            }
            else
            {
                break;
            }
        }
        
        toMergeBlockStateSets = toMergeBlockStateSets.OrderBy(o => o.Block.BlockHeight).ToList();

        await _appStateProvider.MergeStateAsync(eventData.ChainId, toMergeBlockStateSets);
        
        _appBlockStateSetProvider.CleanBlockStateSets(eventData.ChainId, eventData.BlockHeight,
            eventData.BlockHash);
    }
}