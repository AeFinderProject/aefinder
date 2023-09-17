using AElfIndexer.BlockScan;
using AElfIndexer.Client.Providers;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfIndexer.Client.Handlers;

public class DAppSubscribedDataHandler: IDistributedEventHandler<SubscribedBlockDto>, ITransientDependency
{
    private readonly IEnumerable<IBlockChainDataHandler> _handlers;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly ILogger<DAppSubscribedDataHandler> _logger;

    public DAppSubscribedDataHandler(IEnumerable<IBlockChainDataHandler> handlers,
        ILogger<DAppSubscribedDataHandler> logger,
        IBlockScanAppService blockScanAppService)
    {
        _handlers = handlers;
        _logger = logger;
        _blockScanAppService = blockScanAppService;
    }

    public async Task HandleEventAsync(SubscribedBlockDto subscribedBlock)
    {
        var clientVersion = await _blockScanAppService.GetClientVersionAsync(subscribedBlock.ClientId);
        var clientToken =
            await _blockScanAppService.GetClientTokenAsync(subscribedBlock.ClientId, subscribedBlock.Version);
        
        if (subscribedBlock.Version != clientVersion.CurrentVersion &&
            subscribedBlock.Version != clientVersion.NewVersion)
        {
            _logger.LogError(
                "DAppSubscribedDataHandler Version not match! subscribedClientId: {subscribedClientId} subscribedVersion: {subscribedVersion} clientCurrentVersion: {clientCurrentVersion} clientNewVersion: {clientNewVersion}  FilterType: {FilterType}, ChainId: {subscribedBlock}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
                subscribedBlock.ClientId, subscribedBlock.Version,
                clientVersion.CurrentVersion, clientVersion.NewVersion,
                subscribedBlock.FilterType, subscribedBlock.Blocks.First().ChainId,
                subscribedBlock.Blocks.First().BlockHeight,
                subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
            return;
        }
        
        if (subscribedBlock.Token != clientToken)
        {
            return;
        }
        
        _logger.LogInformation(
            "Receive {ClientId} subscribedBlock: Version: {Version} FilterType: {FilterType}, ChainId: {subscribedBlock}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
            subscribedBlock.ClientId,subscribedBlock.Version,subscribedBlock.FilterType, subscribedBlock.Blocks.First().ChainId, subscribedBlock.Blocks.First().BlockHeight,
            subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
        
        var handler = _handlers.First(h => h.FilterType == subscribedBlock.FilterType);
        await handler.HandleBlockChainDataAsync(subscribedBlock.ChainId, subscribedBlock.ClientId, subscribedBlock.Blocks);
        
        
    }
}