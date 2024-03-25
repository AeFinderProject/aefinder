using AeFinder.App.BlockProcessing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AeFinder.App.Handlers;

public class LongestChainFoundHandler : ILocalEventHandler<LongestChainFoundEventData>,
    ITransientDependency
{
    private readonly IBlockProcessingService _blockProcessingService;

    public LongestChainFoundHandler(IBlockProcessingService blockProcessingService)
    {
        _blockProcessingService = blockProcessingService;
    }

    public async Task HandleEventAsync(LongestChainFoundEventData eventData)
    {
        await _blockProcessingService.ProcessAsync(eventData.ChainId, eventData.BlockHash);
    }
}