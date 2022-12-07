using System.Runtime.Serialization;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Entities.Es;
using AElfIndexer.Etos;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Orleans;
using AElfIndexer.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer;

public interface IBlockIndexHandler
{
    Task ProcessNewBlockAsync(BlockWithTransactionDto block);
    Task ProcessConfirmedBlocksAsync(List<BlockWithTransactionDto> confirmBlocks);
}

public class BlockIndexHandler : IBlockIndexHandler, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockAppService _blockAppService;
    
    public ILogger<BlockIndexHandler> Logger { get; set; }

    public BlockIndexHandler(IObjectMapper objectMapper, IClusterClient clusterClient, IBlockAppService blockAppService)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _blockAppService = blockAppService;

        Logger = NullLogger<BlockIndexHandler>.Instance;
    }

    public async Task ProcessNewBlockAsync(BlockWithTransactionDto block)
    {
        try
        {
            var chainGrain = _clusterClient.GetGrain<IChainGrain>(block.ChainId);
            await chainGrain.SetLatestBlockAsync(block.BlockHash, block.BlockHeight);
            
            var clientManagerGrain = _clusterClient.GetGrain<IClientManagerGrain>(0);
            var clientIds = await clientManagerGrain.GetClientIdsByChainAsync(block.ChainId);
            var tasks = clientIds.Select(async clientId =>
            {
                var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
                var clientInfo = await clientGrain.GetClientInfoAsync();
                if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                    clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= block.BlockHeight)
                {
                    var blockScanGrain = _clusterClient.GetGrain<IBlockScanGrain>(clientId);
                    await blockScanGrain.HandleNewBlockAsync(block);
                }
            });

            await tasks.WhenAll();
        }
        catch (Exception e)
        {
            Logger.LogError(e,$"Process new block failed.");
            throw;
        }
    }

    public async Task ProcessConfirmedBlocksAsync(List<BlockWithTransactionDto> confirmBlocks)
    {
        try
        {
            var firstBlock = confirmBlocks.First();
            var chainId = firstBlock.ChainId;

            var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (chainStatus.ConfirmedBlockHeight >= firstBlock.BlockHeight)
            {
                return;
            }

            if (chainStatus.ConfirmedBlockHash != firstBlock.PreviousBlockHash ||
                chainStatus.ConfirmedBlockHeight != firstBlock.BlockHeight - 1)
            {
                var start = chainStatus.ConfirmedBlockHeight + 1;
                var end = firstBlock.BlockHeight - 1;
                var count = await _blockAppService.GetBlockCountAsync(new GetBlocksInput
                {
                    ChainId = chainId,
                    IsOnlyConfirmed = true,
                    StartBlockHeight = start,
                    EndBlockHeight = end
                });
                if (count != end - start + 1)
                {
                    return;
                }
            }

            await chainGrain.SetLatestConfirmBlockAsync(confirmBlocks.Last().BlockHash,
                confirmBlocks.Last().BlockHeight);

            var clientManagerGrain = _clusterClient.GetGrain<IClientManagerGrain>(0);
            var clientIds = await clientManagerGrain.GetClientIdsByChainAsync(chainId);
            var tasks = clientIds.Select(async clientId =>
            {
                var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
                var clientInfo = await clientGrain.GetClientInfoAsync();
                if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                    clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= confirmBlocks.First().BlockHeight)
                {
                    var blockScanGrain = _clusterClient.GetGrain<IBlockScanGrain>(clientId);
                    await blockScanGrain.HandleConfirmedBlockAsync(confirmBlocks);
                }
            });

            await tasks.WhenAll();
        }
        catch (Exception e)
        {
            Logger.LogError(e,$"Process Confirmed block failed.");
            throw;
        }
    }
}