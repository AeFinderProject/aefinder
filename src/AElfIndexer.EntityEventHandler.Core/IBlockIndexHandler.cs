using System;
using System.Linq;
using AElfIndexer.Block;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockScanExecution;
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
    Task ProcessConfirmedBlocksAsync(BlockWithTransactionDto confirmBlock);
}

public class BlockIndexHandler : IBlockIndexHandler, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockAppService _blockAppService;
    private const int MaxRequestBlockCount = 1000;

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
            
            var clientManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(GrainIdHelper.GenerateBlockScanManagerGrainId());
            var ids = await clientManagerGrain.GetBlockScanIdsByChainAsync(block.ChainId);
            var tasks = ids.Select(async id =>
            {
                var clientGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);
                if (await clientGrain.IsScanBlockAsync(block.BlockHeight,false))
                {
                    Logger.LogDebug(
                        "HandleBlock: {ChainId} Client: {id} BlockHeight: {BlockHeight} BlockHash: {BlockHash}",
                        block.ChainId, id, block.BlockHeight, block.BlockHash);
                    var blockScanGrain = _clusterClient.GetGrain<IBlockScanExecutorGrain>(id);
                    await blockScanGrain.HandleBlockAsync(block);
                }
            });

            await tasks.WhenAll();
        }
        catch (Exception e)
        {
            Logger.LogError(e,"Process new block failed.");
            throw;
        }
    }

    public async Task ProcessConfirmedBlocksAsync(BlockWithTransactionDto confirmBlock)
    {
        try
        {
            var chainId = confirmBlock.ChainId;

            var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (chainStatus.ConfirmedBlockHeight >= confirmBlock.BlockHeight)
            {
                Logger.LogDebug(
                    "Drop old block, ChainId: {chainId} ConfirmedBlockHeight: {ConfirmedBlockHeight} BlockHeight: {BlockHeight}",
                    chainId, chainStatus.ConfirmedBlockHeight, confirmBlock.BlockHeight);
                return;
            }

            if (chainStatus.ConfirmedBlockHeight != 0 &&
                (chainStatus.ConfirmedBlockHash != confirmBlock.PreviousBlockHash ||
                 chainStatus.ConfirmedBlockHeight != confirmBlock.BlockHeight - 1))
            {
                var start = chainStatus.ConfirmedBlockHeight + 1;
                var end = Math.Min(confirmBlock.BlockHeight - 1, start + MaxRequestBlockCount - 1);
                while (true)
                {
                    var count = await _blockAppService.GetBlockCountAsync(new GetBlocksInput
                    {
                        ChainId = chainId,
                        IsOnlyConfirmed = true,
                        StartBlockHeight = start,
                        EndBlockHeight = end
                    });
                    if (count != end - start + 1)
                    {
                        Logger.LogWarning(
                            "Wrong confirmed block count, ChainId: {chainId} StartBlockHeight: {start} EndBlockHeight: {end} Count: {count}",
                            chainId, start, end, count);
                        return;
                    }

                    if (end == confirmBlock.BlockHeight - 1)
                    {
                        break;
                    }

                    var blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
                    {
                        ChainId = chainId,
                        IsOnlyConfirmed = true,
                        StartBlockHeight = end,
                        EndBlockHeight = end
                    });

                    await chainGrain.SetLatestConfirmBlockAsync(blocks[0].BlockHash, blocks[0].BlockHeight);

                    start = end + 1;
                    end = Math.Min(confirmBlock.BlockHeight - 1, start + MaxRequestBlockCount - 1);
                }
            }

            await chainGrain.SetLatestConfirmBlockAsync(confirmBlock.BlockHash, confirmBlock.BlockHeight);

            var clientManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(GrainIdHelper.GenerateBlockScanManagerGrainId());
            var ids = await clientManagerGrain.GetBlockScanIdsByChainAsync(chainId);
            var tasks = ids.Select(async id =>
            {
                var clientGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);
                if (await clientGrain.IsScanBlockAsync(confirmBlock.BlockHeight,true))
                {
                    Logger.LogDebug("HandleConfirmedBlock: {chainId} Client: {id} BlockHeight: {BlockHeight}", chainId,
                        id, confirmBlock.BlockHeight);
                    var blockScanGrain = _clusterClient.GetGrain<IBlockScanExecutorGrain>(id);
                    await blockScanGrain.HandleConfirmedBlockAsync(confirmBlock);
                }
            });

            await tasks.WhenAll();
        }
        catch (Exception e)
        {
            Logger.LogError(e,"Process Confirmed block failed.");
            throw;
        }
    }
}