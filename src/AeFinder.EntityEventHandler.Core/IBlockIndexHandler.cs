using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Block;
using AeFinder.Block.Dtos;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Chains;
using AElf.ExceptionHandler;
using AElf.OpenTelemetry.ExecutionTime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.EntityEventHandler;

public interface IBlockIndexHandler
{
    Task ProcessNewBlockAsync(BlockWithTransactionDto block);
    Task ProcessConfirmedBlocksAsync(BlockWithTransactionDto confirmBlock);
}

[AggregateExecutionTime]
public partial class BlockIndexHandler : IBlockIndexHandler, ISingletonDependency
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

    [ExceptionHandler([typeof(Exception)], Message = "Process new block failed.", LogOnly = true)]
    public virtual async Task ProcessNewBlockAsync(BlockWithTransactionDto block)
    {
        var chainGrain = _clusterClient.GetGrain<IChainGrain>(block.ChainId);
        await chainGrain.SetLatestBlockAsync(block.BlockHash, block.BlockHeight);

        var clientManagerGrain =
            _clusterClient.GetGrain<IBlockPusherManagerGrain>(GrainIdHelper.GenerateBlockPusherManagerGrainId());
        var ids = await clientManagerGrain.GetBlockPusherIdsByChainAsync(block.ChainId);
        var tasks = ids.Select(async id =>
        {
            var clientGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            if (await clientGrain.IsPushBlockAsync(block.BlockHeight, false))
            {
                Logger.LogDebug(
                    "HandleBlock: {ChainId} Client: {id} BlockHeight: {BlockHeight} BlockHash: {BlockHash}",
                    block.ChainId, id, block.BlockHeight, block.BlockHash);
                var blockPusherGrain = _clusterClient.GetGrain<IBlockPusherGrain>(id);
                await blockPusherGrain.HandleBlockAsync(block);
            }
        });

        await tasks.WhenAll();
    }

    [ExceptionHandler([typeof(Exception)], Message = "Process Confirmed block failed.", LogOnly = true)]
    public virtual async Task ProcessConfirmedBlocksAsync(BlockWithTransactionDto confirmBlock)
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

                await chainGrain.SetLatestConfirmedBlockAsync(blocks[0].BlockHash, blocks[0].BlockHeight);

                start = end + 1;
                end = Math.Min(confirmBlock.BlockHeight - 1, start + MaxRequestBlockCount - 1);
            }
        }

        await chainGrain.SetLatestConfirmedBlockAsync(confirmBlock.BlockHash, confirmBlock.BlockHeight);

        var clientManagerGrain =
            _clusterClient.GetGrain<IBlockPusherManagerGrain>(GrainIdHelper.GenerateBlockPusherManagerGrainId());
        var ids = await clientManagerGrain.GetBlockPusherIdsByChainAsync(chainId);
        var tasks = ids.Select(async id =>
        {
            var clientGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            if (await clientGrain.IsPushBlockAsync(confirmBlock.BlockHeight, true))
            {
                Logger.LogDebug("HandleConfirmedBlock: {chainId} Client: {id} BlockHeight: {BlockHeight}", chainId,
                    id, confirmBlock.BlockHeight);
                var blockScanGrain = _clusterClient.GetGrain<IBlockPusherGrain>(id);
                await blockScanGrain.HandleConfirmedBlockAsync(confirmBlock);
            }
        });

        await tasks.WhenAll();
    }
}