using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Block;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Entities.Es;
using AElfIndexer.Etos;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
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
            
            var clientManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(0);
            var clientIds = await clientManagerGrain.GetBlockScanIdsByChainAsync(block.ChainId);
            var tasks = clientIds.Select(async clientId =>
            {
                var clientGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(clientId);
                var clientInfo = await clientGrain.GetClientInfoAsync();
                if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                    clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= block.BlockHeight)
                {
                    Logger.LogDebug($"HandleConfirmedBlock: {block.ChainId} Client: {clientId} BlockHeight: {block.BlockHeight} BlockHash: {block.BlockHash}");
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
                    Logger.LogDebug($"Wrong confirmed block count, ChainId: {chainId} StartBlockHeight: {start} EndBlockHeight: {end}");
                    return;
                }
            }

            await chainGrain.SetLatestConfirmBlockAsync(confirmBlocks.Last().BlockHash,
                confirmBlocks.Last().BlockHeight);

            var clientManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(0);
            var clientIds = await clientManagerGrain.GetBlockScanIdsByChainAsync(chainId);
            var tasks = clientIds.Select(async clientId =>
            {
                var clientGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(clientId);
                var clientInfo = await clientGrain.GetClientInfoAsync();
                if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                    clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= confirmBlocks.First().BlockHeight)
                {
                    Logger.LogDebug($"HandleConfirmedBlock: {chainId} Client: {clientId} StartBlockHeight: {firstBlock.BlockHeight} EndBlockHeight: {confirmBlocks.Last().BlockHeight}");
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