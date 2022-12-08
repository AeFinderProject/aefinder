using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockScan;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandler<T> : ISubscribedBlockHandler<T>, ISingletonDependency
{
    private readonly IEnumerable<IBlockChainDataHandler<T>> _handlers;
    private readonly IClusterClient _clusterClient;
    public ILogger<SubscribedBlockHandler<T>> Logger { get; set; }

    public SubscribedBlockHandler(IEnumerable<IBlockChainDataHandler<T>> handlers, IClusterClient clusterClient)
    {
        _handlers = handlers;
        _clusterClient = clusterClient;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;

        var clientGrain = _clusterClient.GetGrain<IClientGrain>(subscribedBlock.ClientId);
        var isVersionAvailable = await clientGrain.IsVersionAvailableAsync(subscribedBlock.Version);
        if (!isVersionAvailable)
        {
            return;
        }

        var handler = _handlers.First(h => h.FilterType == subscribedBlock.FilterType);
        await handler.HandleBlockChainDataAsync(subscribedBlock.ChainId, subscribedBlock.ClientId, subscribedBlock.Blocks);
    }
}