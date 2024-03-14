using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockStates;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Grains.Grain.Subscriptions;
using AElfIndexer.Grains.State.BlockPush;
using AElfIndexer.Grains.State.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Grains.Grain.BlockPush;

public class BlockPusherGrain : Grain<BlockPusherState>, IBlockPusherGrain
{
    private readonly IBlockFilterProvider _blockFilterProvider;
    private readonly BlockPushOptions _blockPushOptions;
    private readonly ILogger<BlockPusherGrain> _logger;
    private readonly IObjectMapper _objectMapper;

    private IAsyncStream<SubscribedBlockDto> _stream = null!;

    private string _appId;
    private string _version;
    private Subscription _subscription;

    public BlockPusherGrain(IOptionsSnapshot<BlockPushOptions> blockPushOptions,
        IBlockFilterProvider blockFilterProvider, ILogger<BlockPusherGrain> logger, IObjectMapper objectMapper)
    {
        _blockFilterProvider = blockFilterProvider;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockPushOptions = blockPushOptions.Value;
    }

    public async Task InitializeAsync(string pushToken, long startHeight)
    {
        State.PushedBlockHeight = startHeight - 1;
        State.PushedBlockHash = null;
        State.PushedConfirmedBlockHeight = startHeight - 1;
        State.PushedConfirmedBlockHash = null;
        State.PushedBlocks = new SortedDictionary<long, HashSet<string>>();
        State.PushToken = pushToken;
        
        var blockPusherInfoGrain = GrainFactory.GetGrain<IBlockPusherInfoGrain>(this.GetPrimaryKeyString());
        await blockPusherInfoGrain.SetHistoricalBlockPushModeAsync();
        
        var pushInfo = await blockPusherInfoGrain.GetPushInfoAsync();
        _appId = pushInfo.AppId;
        _version = pushInfo.Version;
        _subscription = await blockPusherInfoGrain.GetSubscriptionAsync();

        await WriteStateAsync();
    }
    
    public async Task HandleHistoricalBlockAsync()
    {
        try
        {
            await ReadStateAsync();

            _logger.LogInformation("Grain: {GrainId} token: {PushToken} pushing block begin from {PushedBlockHeight}",
                this.GetPrimaryKeyString(), State.PushToken, State.PushedBlockHeight);

            var pusherInfoGrain = GrainFactory.GetGrain<IBlockPusherInfoGrain>(this.GetPrimaryKeyString());
            var appSubscriptionGrain = GrainFactory.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(_appId));
            var chainGrain = GrainFactory.GetGrain<IChainGrain>(_subscription.ChainId);
            var chainStatus = await chainGrain.GetChainStatusAsync();

            while (true)
            {
                if (!await appSubscriptionGrain.IsRunningAsync(_version, _subscription.ChainId, State.PushToken) ||
                    await pusherInfoGrain.GetPushModeAsync() != BlockPushMode.HistoricalBlock ||
                    !await CheckPushThresholdAsync(_subscription.StartBlockNumber, State.PushedConfirmedBlockHeight, _blockPushOptions.MaxHistoricalBlockPushThreshold))
                {
                    break;
                }

                await pusherInfoGrain.SetHandleHistoricalBlockTimeAsync(DateTime.UtcNow);

                if (State.PushedConfirmedBlockHeight >=
                    chainStatus.ConfirmedBlockHeight - _blockPushOptions.PushHistoryBlockThreshold)
                {
                    _logger.LogInformation(
                        "Grain: {GrainId} token: {PushToken} switch to new block mode. PushedConfirmedBlockHeight: {PushedConfirmedBlockHeight}, ConfirmedBlockHeight: {ConfirmedBlockHeight}, PushHistoryBlockThreshold: {PushHistoryBlockThreshold}",
                        this.GetPrimaryKeyString(), State.PushToken, State.PushedConfirmedBlockHeight,
                        chainStatus.ConfirmedBlockHeight, _blockPushOptions.PushHistoryBlockThreshold);
                    await pusherInfoGrain.SetNewBlockStartHeightAsync(State.PushedConfirmedBlockHeight + 1);
                    break;
                }

                var targetHeight = Math.Min(State.PushedConfirmedBlockHeight + _blockPushOptions.BatchPushBlockCount,
                    chainStatus.ConfirmedBlockHeight - _blockPushOptions.PushHistoryBlockThreshold);

                var blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
                {
                    ChainId = _subscription.ChainId,
                    StartBlockHeight = State.PushedConfirmedBlockHeight + 1,
                    EndBlockHeight = targetHeight,
                    IsOnlyConfirmed = true,
                    TransactionFilters = _objectMapper.Map<List<TransactionCondition>, List<FilterTransactionInput>>(_subscription.TransactionConditions),
                    LogEventFilters = _objectMapper.Map<List<LogEventCondition>, List<FilterContractEventInput>>(_subscription.LogEventConditions)
                });

                if (blocks.Count != targetHeight - State.PushedConfirmedBlockHeight)
                {
                    throw new ApplicationException(
                        $"Cannot fill vacant blocks: from {State.PushedConfirmedBlockHeight + 1} to {targetHeight}");
                }

                if (!_subscription.OnlyConfirmed)
                {
                    SetConfirmed(blocks, false);
                    await PushBlocksAsync(blocks);
                }

                SetConfirmed(blocks, true);
                await PushBlocksAsync(blocks);

                State.PushedBlockHeight = blocks.Last().BlockHeight;
                State.PushedBlockHash = blocks.Last().BlockHash;
                State.PushedConfirmedBlockHeight = blocks.Last().BlockHeight;;
                State.PushedConfirmedBlockHash = blocks.Last().BlockHash;

                await WriteStateAsync();

                chainStatus = await chainGrain.GetChainStatusAsync();
                _logger.LogInformation(
                    "Grain: {GrainId} token: {PushToken} pushed historical block from {BeginBlockHeight} to {EndBlockHeight}",
                    this.GetPrimaryKeyString(), State.PushToken, blocks.First().BlockHeight, blocks.Last().BlockHeight);

            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Grain: {GrainId} token: {PushToken} handle historical block failed: {Message}",
                this.GetPrimaryKeyString(), State.PushToken, e.Message);
            throw;
        }
    }

    public async Task HandleBlockAsync(BlockWithTransactionDto block)
    {
        if (!CheckPushThreshold(block.BlockHeight))
        {
            return;
        }
        
        await ReadStateAsync();

        var appSubscriptionGrain = GrainFactory.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(_appId));
        var pusherInfoGrain = GrainFactory.GetGrain<IBlockPusherInfoGrain>(this.GetPrimaryKeyString());

        if (!await CheckPushThresholdAsync(_subscription.StartBlockNumber, State.PushedBlockHeight, _blockPushOptions.MaxNewBlockPushThreshold))
        {
            return;
        }

        if (_subscription.OnlyConfirmed
            || await pusherInfoGrain.GetPushModeAsync() != BlockPushMode.NewBlock
            || !await appSubscriptionGrain.IsRunningAsync(_version, _subscription.ChainId, State.PushToken))
        {
            _logger.LogWarning(
                "Grain: {GrainId} token: {PushToken} handle block failed, block height: {BlockHeight}, block hash: {BlockHash}",
                this.GetPrimaryKeyString(), State.PushToken, block.BlockHeight, block.BlockHash);
            return;
        }

        var blocks = new List<BlockWithTransactionDto>();
        var needCheckIncomplete = true;
        var startHeight = State.PushedBlockHeight + 1;
        if (block.BlockHeight == State.PushedBlockHeight + 1 && block.PreviousBlockHash == State.PushedBlockHash)
        {
            blocks.Add(block);
            needCheckIncomplete = false;
        }
        else if (State.PushedBlocks.Count == 0)
        {
            blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = _subscription.ChainId,
                StartBlockHeight = startHeight,
                EndBlockHeight = GetMaxTargetHeight(startHeight, block.BlockHeight),
                IsOnlyConfirmed = false
            });
        }
        else if (State.PushedBlocks.TryGetValue(block.BlockHeight - 1, out var previousBlocks) &&
                 previousBlocks.Contains(block.PreviousBlockHash))
        {
            blocks.Add(block);
            needCheckIncomplete = false;
        }
        else
        {
            startHeight = GetMinPushBlockHeight();
            _logger.LogDebug(
                "Grain: {GrainId} token: {PushToken} found not linked block, block height: {BlockHeight}, block hash: {BlockHash}, from height: {startHeight}",
                this.GetPrimaryKeyString(), State.PushToken, block.BlockHeight, block.BlockHash, startHeight);
            blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = _subscription.ChainId,
                StartBlockHeight = startHeight,
                EndBlockHeight = GetMaxTargetHeight(startHeight, block.BlockHeight),
                IsOnlyConfirmed = false
            });
        }
        
        if (blocks.Count == 0)
        {
            var message =
                $"Grain: {this.GetPrimaryKeyString()} token: {State.PushToken} cannot get blocks: from {startHeight} to {block.BlockHeight}";
            _logger.LogError(message);
            throw new ApplicationException(message);
        }

        var unPushedBlock = await GetUnPushedBlocksAsync(blocks, needCheckIncomplete);
        if (unPushedBlock.Count == 0)
        {
            _logger.LogWarning(
                "Grain: {GrainId} token: {PushToken} handle block failed, no unpushed block. block height: {BlockHeight}, block hash: {BlockHash}",
                this.GetPrimaryKeyString(), State.PushToken, block.BlockHeight, block.BlockHash);
            return;
        }

        State.PushedBlockHeight = unPushedBlock.Last().BlockHeight;
        State.PushedBlockHash = unPushedBlock.Last().BlockHash;

        var subscribedBlocks = await _blockFilterProvider.FilterBlocksAsync(unPushedBlock, _subscription.TransactionConditions,
            _subscription.LogEventConditions);
        
        SetConfirmed(subscribedBlocks, false);
        await PushBlocksAsync(subscribedBlocks);

        await WriteStateAsync();
        _logger.LogInformation("Grain: {GrainId} token: {PushToken} pushed block from {First} to {Last}",
            this.GetPrimaryKeyString(), State.PushToken, subscribedBlocks.First().BlockHeight,
            subscribedBlocks.Last().BlockHeight);
    }

    public async Task HandleConfirmedBlockAsync(BlockWithTransactionDto block)
    {
        if (!CheckPushThreshold(block.BlockHeight))
        {
            return;
        }

        await ReadStateAsync();

        var appSubscriptionGrain = GrainFactory.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(_appId));
        var pusherInfoGrain = GrainFactory.GetGrain<IBlockPusherInfoGrain>(this.GetPrimaryKeyString());
        
        if (!await CheckPushThresholdAsync(_subscription.StartBlockNumber, State.PushedConfirmedBlockHeight, _blockPushOptions.MaxNewBlockPushThreshold))
        {
            return;
        }
        
        if (block.BlockHeight <= State.PushedConfirmedBlockHeight
            || await pusherInfoGrain.GetPushModeAsync() != BlockPushMode.NewBlock
            || !await appSubscriptionGrain.IsRunningAsync(_version, _subscription.ChainId, State.PushToken))
        {
            _logger.LogWarning(
                "Grain: {GrainId} token: {PushToken} handle confirmed block failed, block height: {BlockHeight}",
                this.GetPrimaryKeyString(), State.PushToken, block.BlockHeight);
            return;
        }
        
        var pushedBlocks = new List<BlockWithTransactionDto>();
        if (block.BlockHeight == State.PushedConfirmedBlockHeight + 1)
        {
            pushedBlocks.Add(block);
        }
        else
        {
            _logger.LogDebug(
                "Grain: {GrainId} token: {PushToken} found not linked confirmed block, block height: {BlockHeight}, block hash: {BlockHash}, current block height: {PushedConfirmedBlockHeight}",
                this.GetPrimaryKeyString(), State.PushToken, block.BlockHeight, block.BlockHash,
                State.PushedConfirmedBlockHeight);

            var startHeight = State.PushedConfirmedBlockHeight + 1;
            pushedBlocks.AddRange(await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = _subscription.ChainId,
                StartBlockHeight = startHeight,
                EndBlockHeight = GetMaxTargetHeight(startHeight, block.BlockHeight),
                IsOnlyConfirmed = true
            }));

            pushedBlocks = await _blockFilterProvider.FilterIncompleteConfirmedBlocksAsync(_subscription.ChainId,
                pushedBlocks,
                State.PushedConfirmedBlockHash, State.PushedConfirmedBlockHeight);
        }

        pushedBlocks =
            await _blockFilterProvider.FilterBlocksAsync(pushedBlocks, _subscription.TransactionConditions,_subscription.LogEventConditions);

        if (pushedBlocks.Count == 0)
        {
            var message =
                $"Grain: {this.GetPrimaryKeyString()} token: {State.PushToken} cannot get confirmed blocks: from {State.PushedConfirmedBlockHeight + 1} to {block.BlockHeight}";
            _logger.LogError(message);
            throw new ApplicationException(message);
        }

        if (!_subscription.OnlyConfirmed && !await IsUnConfirmedBlockPushedAsync(pushedBlocks))
        {
            return;
        }

        SetConfirmed(pushedBlocks, true);
        await PushBlocksAsync(pushedBlocks);

        State.PushedConfirmedBlockHeight = pushedBlocks.Last().BlockHeight;
        State.PushedConfirmedBlockHash = pushedBlocks.Last().BlockHash;
        await WriteStateAsync();

        _logger.LogInformation(
            "Grain: {GrainId} token: {PushToken} pushed confirmed block from {FromBlockHeight} to {ToBlockHeight}",
            this.GetPrimaryKeyString(), State.PushToken, pushedBlocks.First().BlockHeight,
            pushedBlocks.Last().BlockHeight);
    }

    private async Task<bool> IsUnConfirmedBlockPushedAsync(List<BlockWithTransactionDto> blocks)
    {
        foreach (var b in blocks)
        {
            if (!State.PushedBlocks.TryGetValue(b.BlockHeight, out var existBlocks) ||
                !existBlocks.Contains(b.BlockHash))
            {
                if (b.BlockHeight < State.PushedBlockHeight)
                {
                    State.PushedBlockHeight = b.BlockHeight - 1;
                    State.PushedBlockHash = b.PreviousBlockHash;
                    await WriteStateAsync();
                }

                return false;
            }

            State.PushedBlocks.RemoveAll(o => o.Key <= b.BlockHeight);
            State.PushedBlockHeight = b.BlockHeight;
        }

        return true;
    }

    private async Task PushBlocksAsync(List<BlockWithTransactionDto> blocks)
    {
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            AppId = _appId,
            ChainId = _subscription.ChainId,
            Version = _version,
            Blocks = blocks,
            PushToken = State.PushToken
        });
    }

    private async Task<List<BlockWithTransactionDto>> GetUnPushedBlocksAsync(List<BlockWithTransactionDto> blocks, bool needCheckIncomplete)
    {
        var unPushedBlock = new List<BlockWithTransactionDto>();
        while (true)
        {
            if (needCheckIncomplete)
            {
                blocks = await _blockFilterProvider.FilterIncompleteBlocksAsync(_subscription.ChainId, blocks);
                if (blocks.Count == 0)
                {
                    _logger.LogWarning(
                        $"Grain: {this.GetPrimaryKeyString()} token: {State.PushToken} no block filtered",
                        this.GetPrimaryKeyString(), State.PushToken);
                    break;
                }
            }

            foreach (var b in blocks)
            {
                var minPushBlockHeight = GetMinPushBlockHeight();
                if (minPushBlockHeight != 0
                    && minPushBlockHeight < b.BlockHeight
                    && (!State.PushedBlocks.TryGetValue(b.BlockHeight - 1, out var prePushedBlocks) ||
                        !prePushedBlocks.Contains(b.PreviousBlockHash)))
                {
                    continue;
                }

                if (!State.PushedBlocks.TryGetValue(b.BlockHeight, out var pushedBlocks))
                {
                    pushedBlocks = new HashSet<string>();
                }

                if (pushedBlocks.Add(b.BlockHash))
                {
                    unPushedBlock.Add(b);
                }

                State.PushedBlocks[b.BlockHeight] = pushedBlocks;
            }

            if (!needCheckIncomplete || unPushedBlock.Count > 0)
            {
                break;
            }

            var startBlockHeight = blocks.Last().BlockHeight + 1;
            blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = _subscription.ChainId,
                StartBlockHeight = startBlockHeight,
                EndBlockHeight = startBlockHeight + _blockPushOptions.BatchPushBlockCount - 1,
                IsOnlyConfirmed = false
            });
            if (blocks.Count == 0)
            {
                break;
            }
        }
        
        return unPushedBlock;
    }
    
    private bool CheckPushThreshold(long blockHeight)
    {
        if (blockHeight < State.PushedBlockHeight + _blockPushOptions.BatchPushNewBlockCount)
        {
            return false;
        }

        return true;
    }

    private long GetMinPushBlockHeight()
    {
        if (State.PushedBlocks.Count == 0)
        {
            return 0;
        }

        return State.PushedBlocks.First().Key;
    }

    private void SetConfirmed(List<BlockWithTransactionDto> blocks, bool confirmed)
    {
        foreach (var block in blocks)
        {
            block.Confirmed = confirmed;
            foreach (var transaction in block.Transactions)
            {
                transaction.Confirmed = confirmed;
                foreach (var logEvent in transaction.LogEvents)
                {
                    logEvent.Confirmed = confirmed;
                }
            }
        }
    }

    private long GetMaxTargetHeight(long startHeight, long endHeight)
    {
        return Math.Min(endHeight, startHeight + _blockPushOptions.BatchPushBlockCount - 1);
    }

    private async Task<long> GetConfirmedBlockHeightAsync()
    {
        var blockStateSetStatusGrain = GrainFactory.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appId, _version,_subscription.ChainId));
        return (await blockStateSetStatusGrain.GetBlockStateSetStatusAsync()).LastIrreversibleBlockHeight;
    }

    private async Task<bool> CheckPushThresholdAsync(long startHeight, long pushedHeight, int maxPushThreshold)
    {
        if (pushedHeight - startHeight <= maxPushThreshold)
        {
            return true;
        }

        var clientConfirmedHeight = await GetConfirmedBlockHeightAsync();
        return clientConfirmedHeight >= pushedHeight - maxPushThreshold;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();

        var pusherInfoGrain = GrainFactory.GetGrain<IBlockPusherInfoGrain>(this.GetPrimaryKeyString());
        var streamId = await pusherInfoGrain.GetMessageStreamIdAsync();
        var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);
        _stream = streamProvider.GetStream<SubscribedBlockDto>(
            streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        await base.OnActivateAsync();
    }
}