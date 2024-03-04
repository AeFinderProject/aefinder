using AElf.Types;
using AElfIndexer.App.Handlers;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains.Grain.BlockStates;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElfIndexer.App.BlockState;

public class BlockAttachService : IBlockAttachService, ITransientDependency
{
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;

    private readonly ILogger<BlockAttachService> _logger;

    public ILocalEventBus LocalEventBus { get; set; }

    public BlockAttachService(IAppBlockStateSetProvider appBlockStateSetProvider,
        ILogger<BlockAttachService> logger)
    {
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _logger = logger;
    }

    public async Task AttachBlocksAsync(string chainId, List<BlockWithTransactionDto> blocks)
    {
        await _appBlockStateSetProvider.InitializeAsync(chainId);

        var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
        var lastIrreversibleBlockStateSet =
            await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
        var lastIrreversibleBlockHeight = lastIrreversibleBlockStateSet?.Block.BlockHeight ?? 0;
        var currentBlockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);

        var newLastIrreversibleBlockStateSet = lastIrreversibleBlockStateSet;
        var newLongestChainBlockStateSet = longestChainBlockStateSet;

        foreach (var block in blocks)
        {
            if (block.BlockHeight <= lastIrreversibleBlockHeight)
            {
                continue;
            }

            var previousBlockStateSet =
                await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, block.PreviousBlockHash);
            if (currentBlockStateSetCount != 0 && previousBlockStateSet == null &&
                block.PreviousBlockHash != Hash.Empty.ToHex())
            {
                _logger.LogWarning(
                    "Previous block {PreviousBlockHash} not found. BlockHeight: {BlockHeight}, BlockHash: {BlockHash}.",
                    block.PreviousBlockHash, block.BlockHeight, block.BlockHash);
                continue;
            }

            if (block.Confirmed)
            {
                if (lastIrreversibleBlockHeight == 0)
                {
                    lastIrreversibleBlockHeight = block.BlockHeight;
                }
                else
                {
                    if (block.BlockHeight != lastIrreversibleBlockHeight + 1 && previousBlockStateSet != null &&
                        !previousBlockStateSet.Block.Confirmed)
                    {
                        _logger.LogWarning(
                            "Missing previous confirmed block. Confirmed block height: {BlockHeight}, lib block height: {lastIrreversibleBlockHeight}.",
                            block.BlockHeight, lastIrreversibleBlockHeight);
                        continue;
                    }

                    lastIrreversibleBlockHeight++;
                }
            }

            var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, block.PreviousBlockHash);
            if (blockStateSet == null)
            {
                blockStateSet = new BlockStateSet
                {
                    Block = block,
                    Changes = new(),
                };
                await _appBlockStateSetProvider.AddBlockStateSetAsync(chainId, blockStateSet);
            }
            else
            {
                if (newLongestChainBlockStateSet == null ||
                    blockStateSet.Block.BlockHeight > newLongestChainBlockStateSet.Block.BlockHeight)
                {
                    newLongestChainBlockStateSet = blockStateSet;
                }

                if (block.Confirmed)
                {
                    blockStateSet.Block.Confirmed = block.Confirmed;
                    await _appBlockStateSetProvider.UpdateBlockStateSetAsync(chainId, blockStateSet);

                    if (blockStateSet.Processed)
                    {
                        newLastIrreversibleBlockStateSet = blockStateSet;
                    }
                }
            }
        }

        if (newLastIrreversibleBlockStateSet != null &&
            newLastIrreversibleBlockStateSet.Block.BlockHeight > lastIrreversibleBlockHeight)
        {
            await _appBlockStateSetProvider.SetLastIrreversibleBlockStateSetAsync(chainId,
                newLastIrreversibleBlockStateSet.Block.BlockHash);
            await _appBlockStateSetProvider.SaveDataAsync(chainId);
            await LocalEventBus.PublishAsync(new LastIrreversibleBlockStateSetFoundEventData
            {
                ChainId = chainId,
                BlockHash = newLastIrreversibleBlockStateSet.Block.BlockHash,
                BlockHeight = newLastIrreversibleBlockStateSet.Block.BlockHeight
            });
        }

        if (longestChainBlockStateSet == null && newLongestChainBlockStateSet != null ||
            newLongestChainBlockStateSet != null &&
            newLongestChainBlockStateSet.Block.BlockHeight > lastIrreversibleBlockHeight)
        {
            await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId,
                newLongestChainBlockStateSet.Block.BlockHash);
            await _appBlockStateSetProvider.SaveDataAsync(chainId);
            await LocalEventBus.PublishAsync(new LastIrreversibleBlockStateSetFoundEventData
            {
                ChainId = chainId,
                BlockHash = newLongestChainBlockStateSet.Block.BlockHash,
                BlockHeight = newLongestChainBlockStateSet.Block.BlockHeight
            });
        }
    }
}