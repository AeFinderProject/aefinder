using AElfIndexer.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandler<T> : ISubscribedBlockHandler<T>, ISingletonDependency
{
    private readonly IEnumerable<IBlockChainDataHandler<T>> _handlers;
    public ILogger<SubscribedBlockHandler<T>> Logger { get; set; }

    public SubscribedBlockHandler(IEnumerable<IBlockChainDataHandler<T>> handlers)
    {
        _handlers = handlers;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;
        var handler = _handlers.First(h => h.FilterType == subscribedBlock.FilterType);
        await handler.HandleBlockChainDataAsync(subscribedBlock.ChainId, subscribedBlock.ClientId, subscribedBlock.Blocks);
    }
}