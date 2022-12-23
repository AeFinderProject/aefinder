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
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IClusterClient _clusterClient;
    public ILogger<SubscribedBlockHandler<T>> Logger { get; set; }
    private readonly string _clientId;

    public SubscribedBlockHandler(IEnumerable<IBlockChainDataHandler<T>> handlers,
        IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider, IBlockScanAppService blockScanAppService,
        IClusterClient clusterClient)
    {
        _handlers = handlers;
        _clientId = aelfIndexerClientInfoProvider.GetClientId();
        _blockScanAppService = blockScanAppService;
        _clusterClient = clusterClient;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;
        if (subscribedBlock.ClientId != _clientId) return;

        var clientVersion = await _blockScanAppService.GetClientVersionAsync(subscribedBlock.ClientId);
        var clientToken =
            await _blockScanAppService.GetClientTokenAsync(subscribedBlock.ClientId, subscribedBlock.Version);
        if (subscribedBlock.Version != clientVersion.CurrentVersion &&
            subscribedBlock.Version != clientVersion.NewVersion && subscribedBlock.Token != clientToken)
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