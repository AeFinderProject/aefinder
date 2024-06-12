using AeFinder.App.BlockState;
using AeFinder.App.Handlers;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Grains;
using AElf.Types;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AeFinder.App.BlockProcessing;

public class BlockAttachService : IBlockAttachService, ITransientDependency
{
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IBlockFilterAppService _blockFilterAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<BlockAttachService> _logger;

    private const int MaxRequestBlockCount = 100;

    public ILocalEventBus LocalEventBus { get; set; }

    public BlockAttachService(IAppBlockStateSetProvider appBlockStateSetProvider,
        ILogger<BlockAttachService> logger, IAppInfoProvider appInfoProvider, IClusterClient clusterClient,
        IBlockFilterAppService blockFilterAppService, IObjectMapper objectMapper)
    {
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _logger = logger;
        _appInfoProvider = appInfoProvider;
        _clusterClient = clusterClient;
        _blockFilterAppService = blockFilterAppService;
        _objectMapper = objectMapper;
    }

    public async Task AttachBlocksAsync(string chainId, List<AppSubscribedBlockDto> blocks)
    {
        await _appBlockStateSetProvider.InitializeAsync(chainId);

        var firstBlock = blocks.First();
        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, firstBlock.BlockHash);
        var previousBlockStateSet =
            await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, firstBlock.PreviousBlockHash);
        var currentBlockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
        if (currentBlockStateSetCount != 0 && previousBlockStateSet == null &&
            firstBlock.PreviousBlockHash != Hash.Empty.ToHex() && blockStateSet == null)
        {
            _logger.LogWarning(
                "Handle unlinked block. BlockHeight: {BlockHeight}, BlockHash: {BlockHash}, Previous BlockHash: {PreviousBlockHash}.",
                firstBlock.BlockHeight, firstBlock.BlockHash, firstBlock.PreviousBlockHash);

            await HandleUnlinkedBlockAsync(chainId, firstBlock);
        }

        await HandleBlocksAsync(chainId, blocks);
    }

    private async Task HandleBlocksAsync(string chainId, List<AppSubscribedBlockDto> blocks)
    {
        var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
        var lastIrreversibleBlockStateSet =
            await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
        var lastIrreversibleBlockHeight = lastIrreversibleBlockStateSet?.Block.BlockHeight ?? 0;
        var currentBlockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);

        var newLastIrreversibleBlockStateSet = lastIrreversibleBlockStateSet;
        var newLongestChainBlockStateSet = longestChainBlockStateSet;
        var newLibHeight = lastIrreversibleBlockHeight;

        foreach (var block in blocks)
        {
            if (block.BlockHeight <= newLibHeight)
            {
                continue;
            }

            var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, block.BlockHash);
            var previousBlockStateSet =
                await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, block.PreviousBlockHash);
            if (currentBlockStateSetCount != 0 && previousBlockStateSet == null &&
                block.PreviousBlockHash != Hash.Empty.ToHex() && blockStateSet == null)
            {
                _logger.LogWarning(
                    "Previous block {PreviousBlockHash} not found. BlockHeight: {BlockHeight}, BlockHash: {BlockHash}.",
                    block.PreviousBlockHash, block.BlockHeight, block.BlockHash);
                continue;
            }

            if (block.Confirmed)
            {
                if (newLibHeight == 0)
                {
                    newLibHeight = block.BlockHeight;
                }
                else
                {
                    if (block.BlockHeight != newLibHeight + 1 && previousBlockStateSet != null &&
                        !previousBlockStateSet.Block.Confirmed)
                    {
                        _logger.LogWarning(
                            "Missing previous confirmed block. Confirmed block height: {BlockHeight}, lib block height: {lastIrreversibleBlockHeight}.",
                            block.BlockHeight, newLibHeight);
                        continue;
                    }

                    newLibHeight++;
                }
            }

            if (blockStateSet == null)
            {
                blockStateSet = new BlockStateSet
                {
                    Block = _objectMapper.Map<AppSubscribedBlockDto, BlockWithTransactionDto>(block),
                    Changes = new()
                };
                await _appBlockStateSetProvider.AddBlockStateSetAsync(chainId, blockStateSet);
            }
            else if (block.Confirmed)
            {
                blockStateSet.Block.Confirmed = block.Confirmed;
                await _appBlockStateSetProvider.UpdateBlockStateSetAsync(chainId, blockStateSet);

                if (blockStateSet.Processed)
                {
                    newLastIrreversibleBlockStateSet = blockStateSet;
                }
            }

            if (newLongestChainBlockStateSet == null ||
                blockStateSet.Block.BlockHeight > newLongestChainBlockStateSet.Block.BlockHeight)
            {
                newLongestChainBlockStateSet = blockStateSet;
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
            newLongestChainBlockStateSet.Block.BlockHeight > longestChainBlockStateSet.Block.BlockHeight)
        {
            await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId,
                newLongestChainBlockStateSet.Block.BlockHash);
            await _appBlockStateSetProvider.SaveDataAsync(chainId);
            await LocalEventBus.PublishAsync(new LongestChainFoundEventData()
            {
                ChainId = chainId,
                BlockHash = newLongestChainBlockStateSet.Block.BlockHash,
                BlockHeight = newLongestChainBlockStateSet.Block.BlockHeight
            });
        }
    }

    private async Task HandleUnlinkedBlockAsync(string chainId, AppSubscribedBlockDto block)
    { 
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        var subscription = await client.GetSubscriptionAsync(_appInfoProvider.Version);
        var subscriptionItem = subscription.SubscriptionItems.First(o => o.ChainId == chainId);

        var lastIrreversibleBlockState = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
        var startBlockHeight = 0L;
        if (lastIrreversibleBlockState != null)
        {
            startBlockHeight = lastIrreversibleBlockState.Block.BlockHeight + 1;
        }
        else
        {
            startBlockHeight = subscriptionItem.StartBlockNumber;
        }

        var previousBlockHash = block.PreviousBlockHash;
        var previousBlockHeight = block.BlockHeight - 1;

        while (true)
        {
            var endBlockHeight = Math.Min(startBlockHeight + MaxRequestBlockCount - 1, block.BlockHeight - 1);
            var blocks = await _blockFilterAppService.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = chainId,
                StartBlockHeight = startBlockHeight,
                EndBlockHeight = endBlockHeight,
                IsOnlyConfirmed = block.Confirmed
            });

            if (block.Confirmed)
            {
                blocks = await _blockFilterAppService.FilterIncompleteConfirmedBlocksAsync(chainId, blocks, previousBlockHash,
                    previousBlockHeight);
            }
            else
            {
                blocks = await _blockFilterAppService.FilterIncompleteBlocksAsync(chainId, blocks);
            }
            
            blocks = await _blockFilterAppService.FilterBlocksAsync(blocks,
                _objectMapper.Map<List<TransactionCondition>, List<FilterTransactionInput>>(subscriptionItem
                    .TransactionConditions),
                _objectMapper.Map<List<LogEventCondition>, List<FilterContractEventInput>>(subscriptionItem
                    .LogEventConditions));

            if (blocks.Count == 0)
            {
                return;
            }

            await HandleBlocksAsync(chainId, _objectMapper.Map<List<BlockWithTransactionDto>, List<AppSubscribedBlockDto>>(blocks));
            
            if (endBlockHeight == block.BlockHeight - 1)
            {
                return;
            }
            startBlockHeight = endBlockHeight + 1;
            previousBlockHash = blocks.Last().BlockHash;
            previousBlockHeight = blocks.Last().BlockHeight;
        }
    }
}