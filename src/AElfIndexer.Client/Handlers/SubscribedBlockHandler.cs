using AElfIndexer.Client.Providers;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandler<T> : ISubscribedBlockHandler<T>, ISingletonDependency
{
    private readonly IEnumerable<IBlockChainDataHandler<T>> _handlers;
    private readonly IAElfIndexerClientInfoProvider<T> _aelfIndexerClientInfoProvider;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IClusterClient _clusterClient;
    public ILogger<SubscribedBlockHandler<T>> Logger { get; set; }

    public SubscribedBlockHandler(IEnumerable<IBlockChainDataHandler<T>> handlers, IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider, IBlockScanAppService blockScanAppService, IClusterClient clusterClient)
    {
        _handlers = handlers;
        _aelfIndexerClientInfoProvider = aelfIndexerClientInfoProvider;
        _blockScanAppService = blockScanAppService;
        _clusterClient = clusterClient;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;
        if (subscribedBlock.ClientId != _aelfIndexerClientInfoProvider.GetClientId()) return;

        var clientVersion = await _blockScanAppService.GetClientVersionAsync(subscribedBlock.ClientId);
        if (subscribedBlock.Version != clientVersion.CurrentVersion &&
            subscribedBlock.Version != clientVersion.NewVersion)
        {
            return;
        }

        var handler = _handlers.First(h => h.FilterType == subscribedBlock.FilterType);
        await handler.HandleBlockChainDataAsync(subscribedBlock.ChainId, subscribedBlock.ClientId, subscribedBlock.Blocks);

        //TODO: This can only check one chain 
        if (subscribedBlock.Version == clientVersion.NewVersion)
        {
            var chainGrain = _clusterClient.GetGrain<IChainGrain>(subscribedBlock.ChainId);
            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (subscribedBlock.Blocks.Last().BlockHeight > chainStatus.BlockHeight - 100)
            {
                await _blockScanAppService.UpgradeVersionAsync(subscribedBlock.ClientId);
            }
        }
    }
}