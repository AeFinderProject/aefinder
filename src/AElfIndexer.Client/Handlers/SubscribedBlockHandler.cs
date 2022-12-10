using AElfIndexer.Client.Providers;
using AElfIndexer.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandler<T> : ISubscribedBlockHandler<T>, ISingletonDependency
{
    private readonly IEnumerable<IBlockChainDataHandler<T>> _handlers;
    public ILogger<SubscribedBlockHandler<T>> Logger { get; set; }
    private readonly string _clientId;

    public SubscribedBlockHandler(IEnumerable<IBlockChainDataHandler<T>> handlers, IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider)
    {
        _handlers = handlers;
        _clientId = aelfIndexerClientInfoProvider.GetClientId();
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;
        if (subscribedBlock.ClientId != _clientId) return;
        var handler = _handlers.First(h => h.FilterType == subscribedBlock.FilterType);
        await handler.HandleBlockChainDataAsync(subscribedBlock.ChainId, subscribedBlock.ClientId, subscribedBlock.Blocks);
    }
}