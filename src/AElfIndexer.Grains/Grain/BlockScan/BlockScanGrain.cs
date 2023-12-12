using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Grains.State.BlockScan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockScanGrain : Grain<BlockScanState>, IBlockScanGrain
{
    private readonly IBlockFilterProvider _blockFilterProvider;
    private readonly BlockScanOptions _blockScanOptions;
    private readonly ILogger<BlockScanGrain> _logger;

    private IAsyncStream<SubscribedBlockDto> _stream = null!;

    public BlockScanGrain(IOptionsSnapshot<BlockScanOptions> blockScanOptions,
        IBlockFilterProvider blockFilterProvider, ILogger<BlockScanGrain> logger)
    {
        _blockFilterProvider = blockFilterProvider;
        _logger = logger;
        _blockScanOptions = blockScanOptions.Value;
    }

    public async Task<Guid> InitializeAsync(string chainId, string clientId, string version)
    {
        State.Version = version;
        State.ClientId = clientId;
        State.ChainId = chainId;
        State.ScannedBlockHeight = 0;
        State.ScannedBlockHash = null;
        State.ScannedConfirmedBlockHeight = 0;
        State.ScannedConfirmedBlockHash = null;
        State.ScannedBlocks = new SortedDictionary<long, HashSet<string>>();
        
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        State.Token = await clientGrain.GetTokenAsync(State.Version);

        var blockScanInfoGrain = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var steamId = await blockScanInfoGrain.GetMessageStreamIdAsync();
        State.MessageStreamId = steamId;

        var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);
        _stream = streamProvider.GetStream<SubscribedBlockDto>(
            steamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        await WriteStateAsync();

        return _stream.Guid;
    }

    public async Task ReScanAsync(long blockHeight)
    {
        State.ScannedBlockHeight = blockHeight;
        State.ScannedBlockHash = null;
        State.ScannedConfirmedBlockHeight = blockHeight;
        State.ScannedConfirmedBlockHash = null;
        State.ScannedBlocks = new SortedDictionary<long, HashSet<string>>();
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        State.Token = await clientGrain.GetTokenAsync(State.Version);
        var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        await blockScanInfo.SetHistoricalBlockScanModeAsync();
        await WriteStateAsync();
    }
    
    public async Task HandleHistoricalBlockAsync()
    {
        try
        {
            await ReadStateAsync();
            
            _logger.LogInformation($"Pushing block [grain: {this.GetPrimaryKeyString()} token: {State.Token}]: begin from {State.ScannedBlockHeight}");

            var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
            var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
            var chainGrain = GrainFactory.GetGrain<IChainGrain>(State.ChainId);
            var subscriptionInfo = await blockScanInfo.GetSubscriptionInfoAsync();
            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (State.ScannedBlockHeight == 0 && State.ScannedConfirmedBlockHeight == 0)
            {
                State.ScannedBlockHeight = subscriptionInfo.StartBlockNumber - 1;
                State.ScannedConfirmedBlockHeight = subscriptionInfo.StartBlockNumber - 1;
                await WriteStateAsync();
            }

            while (true)
            {
                if (!await clientGrain.IsRunningAsync(State.Version, State.Token) ||
                    (await blockScanInfo.GetClientInfoAsync()).ScanModeInfo.ScanMode != ScanMode.HistoricalBlock ||
                    !await CheckPushThresholdAsync(subscriptionInfo.StartBlockNumber, State.ScannedConfirmedBlockHeight, _blockScanOptions.MaxHistoricalBlockPushThreshold))
                {
                    break;
                }

                await blockScanInfo.SetHandleHistoricalBlockTimeAsync(DateTime.UtcNow);

                if (State.ScannedConfirmedBlockHeight >=
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold)
                {
                    _logger.LogInformation(
                        $"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: switch to new block mode. ScannedConfirmedBlockHeight: {State.ScannedConfirmedBlockHeight}, ConfirmedBlockHeight: {chainStatus.ConfirmedBlockHeight}, ScanHistoryBlockThreshold: {_blockScanOptions.ScanHistoryBlockThreshold}");
                    await blockScanInfo.SetScanNewBlockStartHeightAsync(State.ScannedConfirmedBlockHeight + 1);
                    break;
                }

                var targetHeight = Math.Min(State.ScannedConfirmedBlockHeight + _blockScanOptions.BatchPushBlockCount,
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold);


                var blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
                {
                    ChainId = State.ChainId,
                    StartBlockHeight = State.ScannedConfirmedBlockHeight + 1,
                    EndBlockHeight = targetHeight,
                    IsOnlyConfirmed = true,
                    TransactionFilters = subscriptionInfo.Transaction,
                    LogEventFilters = subscriptionInfo.LogEvent
                });

                if (blocks.Count != targetHeight - State.ScannedConfirmedBlockHeight)
                {
                    throw new ApplicationException(
                        $"Cannot fill vacant blocks: from {State.ScannedConfirmedBlockHeight + 1} to {targetHeight}");
                }

                if (!subscriptionInfo.OnlyConfirmed)
                {
                    SetConfirmed(blocks, false);
                    await _stream.OnNextAsync(new SubscribedBlockDto
                    {
                        ClientId = State.ClientId,
                        ChainId = State.ChainId,
                        Version = State.Version,
                        Blocks = blocks,
                        Token = State.Token
                    });
                }

                SetConfirmed(blocks, true);
                await _stream.OnNextAsync(new SubscribedBlockDto
                {
                    ClientId = State.ClientId,
                    ChainId = State.ChainId,
                    Version = State.Version,
                    Blocks = blocks,
                    Token = State.Token
                });

                State.ScannedBlockHeight = blocks.Last().BlockHeight;
                State.ScannedBlockHash = blocks.Last().BlockHash;
                State.ScannedConfirmedBlockHeight = blocks.Last().BlockHeight;;
                State.ScannedConfirmedBlockHash = blocks.Last().BlockHash;

                await WriteStateAsync();

                chainStatus = await chainGrain.GetChainStatusAsync();
                _logger.LogInformation($"Pushing block [grain: {this.GetPrimaryKeyString()} token: {State.Token}]: pushed historical block from {blocks.First().BlockHeight} to {blocks.Last().BlockHeight}");

            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: HandleHistoricalBlock failed: {e.Message}");
            throw;
        }
    }

    public async Task HandleBlockAsync(BlockWithTransactionDto block)
    {
        await ReadStateAsync();

        if (block.BlockHeight < State.ScannedBlockHeight + _blockScanOptions.BatchPushNewBlockCount)
        {
            return;
        }

        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var subscriptionInfo = await blockScanInfo.GetSubscriptionInfoAsync();

        if (!await CheckPushThresholdAsync(subscriptionInfo.StartBlockNumber, State.ScannedBlockHeight, _blockScanOptions.MaxNewBlockPushThreshold))
        {
            return;
        }

        if (subscriptionInfo.OnlyConfirmed
            || (await blockScanInfo.GetClientInfoAsync()).ScanModeInfo.ScanMode != ScanMode.NewBlock
            || !await clientGrain.IsRunningAsync(State.Version, State.Token))
        {
            _logger.LogWarning($"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: HandleNewBlock failed, block height: {block.BlockHeight}, block hash: {block.BlockHash}");
            return;
        }

        var blocks = new List<BlockWithTransactionDto>();
        var needCheckIncomplete = true;
        var startHeight = State.ScannedBlockHeight + 1;
        if (block.BlockHeight == State.ScannedBlockHeight + 1 && block.PreviousBlockHash == State.ScannedBlockHash)
        {
            blocks.Add(block);
            needCheckIncomplete = false;
        }
        else if (State.ScannedBlocks.Count == 0)
        {
            blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = State.ChainId,
                StartBlockHeight = startHeight,
                EndBlockHeight = GetMaxTargetHeight(startHeight, block.BlockHeight),
                IsOnlyConfirmed = false
            });
        }
        else if (State.ScannedBlocks.TryGetValue(block.BlockHeight - 1, out var previousBlocks) &&
                 previousBlocks.Contains(block.PreviousBlockHash))
        {
            blocks.Add(block);
            needCheckIncomplete = false;
        }
        else
        {
            startHeight = GetMinScannedBlockHeight();
            _logger.LogDebug($"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: Not linked new block, block height: {block.BlockHeight}, block hash: {block.BlockHash}, from height: {startHeight}");
            blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = State.ChainId,
                StartBlockHeight = startHeight,
                EndBlockHeight = GetMaxTargetHeight(startHeight, block.BlockHeight),
                IsOnlyConfirmed = false
            });
        }
        
        if (blocks.Count == 0)
        {
            var message =
                $"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: Cannot get blocks: from {startHeight} to {block.BlockHeight}";
            _logger.LogError(message);
            throw new ApplicationException(message);
        }

        var unPushedBlock = await GetUnPushedBlocksAsync(blocks, needCheckIncomplete);
        if (unPushedBlock.Count == 0)
        {
            _logger.LogWarning($"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: HandleNewBlock failed, no unpushed block. block height: {block.BlockHeight}, block hash: {block.BlockHash}");
            return;
        }

        State.ScannedBlockHeight = unPushedBlock.Last().BlockHeight;
        State.ScannedBlockHash = unPushedBlock.Last().BlockHash;

        var subscribedBlocks = await _blockFilterProvider.FilterBlocksAsync(unPushedBlock, subscriptionInfo.Transaction,
            subscriptionInfo.LogEvent);
        
        SetConfirmed(subscribedBlocks, false);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            Blocks = subscribedBlocks,
            Token = State.Token
        });

        await WriteStateAsync();
        _logger.LogInformation($"Pushing block [grain: {this.GetPrimaryKeyString()} token: {State.Token}]: pushed new block from {subscribedBlocks.First().BlockHeight} to {subscribedBlocks.Last().BlockHeight}");
    }

    public async Task HandleConfirmedBlockAsync(BlockWithTransactionDto block)
    {
        await ReadStateAsync();
        
        if (block.BlockHeight < State.ScannedConfirmedBlockHeight + _blockScanOptions.BatchPushNewBlockCount)
        {
            return;
        }
        
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var subscriptionInfo = await blockScanInfo.GetSubscriptionInfoAsync();
        
        if (!await CheckPushThresholdAsync(subscriptionInfo.StartBlockNumber, State.ScannedConfirmedBlockHeight, _blockScanOptions.MaxNewBlockPushThreshold))
        {
            return;
        }
        
        if (block.BlockHeight <= State.ScannedConfirmedBlockHeight
            || (await blockScanInfo.GetClientInfoAsync()).ScanModeInfo.ScanMode != ScanMode.NewBlock
            || !await clientGrain.IsRunningAsync(State.Version, State.Token))
        {
            _logger.LogWarning($"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: HandleConfirmedBlock failed, block height: {block.BlockHeight}");
            return;
        }
        
        var scannedBlocks = new List<BlockWithTransactionDto>();
        if (block.BlockHeight == State.ScannedConfirmedBlockHeight + 1)
        {
            scannedBlocks.Add(block);
        }
        else
        {
            _logger.LogDebug(
                $"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: Not linked confirmed block, block height: {block.BlockHeight}, block hash: {block.BlockHash}, current block height: {State.ScannedConfirmedBlockHeight}");
            var startHeight = State.ScannedConfirmedBlockHeight + 1;
            scannedBlocks.AddRange(await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = State.ChainId,
                StartBlockHeight = startHeight,
                EndBlockHeight = GetMaxTargetHeight(startHeight, block.BlockHeight),
                IsOnlyConfirmed = true
            }));
            
            scannedBlocks = await _blockFilterProvider.FilterIncompleteConfirmedBlocksAsync(State.ChainId, scannedBlocks,
                State.ScannedConfirmedBlockHash, State.ScannedConfirmedBlockHeight);
        }
        
        scannedBlocks =
            await _blockFilterProvider.FilterBlocksAsync(scannedBlocks, subscriptionInfo.Transaction,subscriptionInfo.LogEvent);

        if (scannedBlocks.Count == 0)
        {
            var message =
                $"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: Cannot get confirmed blocks: from {State.ScannedConfirmedBlockHeight + 1} to {block.BlockHeight}";
            _logger.LogError(message);
            throw new ApplicationException(message);
        }

        if (!subscriptionInfo.OnlyConfirmed)
        {
            foreach (var b in scannedBlocks)
            {
                if (!State.ScannedBlocks.TryGetValue(b.BlockHeight, out var existBlocks) ||
                    !existBlocks.Contains(b.BlockHash))
                {
                    if (b.BlockHeight < State.ScannedBlockHeight)
                    {
                        State.ScannedBlockHeight = b.BlockHeight - 1;
                        State.ScannedBlockHash = b.PreviousBlockHash;
                        await WriteStateAsync();
                    }

                    return;
                }
                else
                {
                    State.ScannedBlocks.RemoveAll(o => o.Key <= b.BlockHeight);
                    State.ScannedBlockHeight = b.BlockHeight;
                }
            }
        }
        
        SetConfirmed(scannedBlocks, true);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            Blocks = scannedBlocks,
            Token = State.Token
        });

        State.ScannedConfirmedBlockHeight = scannedBlocks.Last().BlockHeight;
        State.ScannedConfirmedBlockHash = scannedBlocks.Last().BlockHash;
        await WriteStateAsync();
        
        _logger.LogInformation($"Pushing block [grain: {this.GetPrimaryKeyString()} token: {State.Token}]: pushed confirmed block from {scannedBlocks.First().BlockHeight} to {scannedBlocks.Last().BlockHeight}");
    }

    private async Task<List<BlockWithTransactionDto>> GetUnPushedBlocksAsync(List<BlockWithTransactionDto> blocks, bool needCheckIncomplete)
    {
        var unPushedBlock = new List<BlockWithTransactionDto>();
        while (true)
        {
            if (needCheckIncomplete)
            {
                blocks = await _blockFilterProvider.FilterIncompleteBlocksAsync(State.ChainId, blocks);
                if (blocks.Count == 0)
                {
                    _logger.LogWarning($"[grain: {this.GetPrimaryKeyString()} token: {State.Token}]: Cannot filter incomplete blocks");
                    break;
                }
            }

            foreach (var b in blocks)
            {
                var minScannedBlockHeight = GetMinScannedBlockHeight();
                if (minScannedBlockHeight != 0
                    && minScannedBlockHeight < b.BlockHeight
                    && (!State.ScannedBlocks.TryGetValue(b.BlockHeight - 1, out var preScannedBlocks) ||
                        !preScannedBlocks.Contains(b.PreviousBlockHash)))
                {
                    continue;
                }

                if (!State.ScannedBlocks.TryGetValue(b.BlockHeight, out var scannedBlocks))
                {
                    scannedBlocks = new HashSet<string>();
                }

                if (scannedBlocks.Add(b.BlockHash))
                {
                    unPushedBlock.Add(b);
                }

                State.ScannedBlocks[b.BlockHeight] = scannedBlocks;
            }

            if (!needCheckIncomplete)
            {
                break;
            }

            if (unPushedBlock.Count > 0)
            {
                break;
            }

            var startBlockHeight = blocks.Last().BlockHeight + 1;
            blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = State.ChainId,
                StartBlockHeight = startBlockHeight,
                EndBlockHeight = startBlockHeight + _blockScanOptions.BatchPushBlockCount - 1,
                IsOnlyConfirmed = false
            });
            if (blocks.Count == 0)
            {
                break;
            }
        }
        
        return unPushedBlock;
    }

    private long GetMinScannedBlockHeight()
    {
        if (State.ScannedBlocks.Count == 0)
        {
            return 0;
        }

        return State.ScannedBlocks.First().Key;
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
        return Math.Min(endHeight, startHeight + _blockScanOptions.BatchPushBlockCount - 1);
    }

    private async Task<long> GetConfirmedBlockHeightAsync()
    {
        // TODO: Get confirmed block height from BlockStateSetInfoGrain
        return 0;
        // var blockStateSetInfoGrain = GrainFactory.GetGrain<IBlockStateSetInfoGrain>(
        //     GrainIdHelper.GenerateGrainId("BlockStateSetInfo", State.ClientId, State.ChainId, State.Version));
        // return await blockStateSetInfoGrain.GetConfirmedBlockHeight(filterType);
    }

    private async Task<bool> CheckPushThresholdAsync(long startHeight, long scannedHeight, int maxPushThreshold)
    {
        if (scannedHeight - startHeight <= maxPushThreshold)
        {
            return true;
        }

        var clientConfirmedHeight = await GetConfirmedBlockHeightAsync();
        return clientConfirmedHeight >= scannedHeight - maxPushThreshold;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();

        if (State.MessageStreamId != Guid.Empty)
        {
            var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);

            _stream = streamProvider.GetStream<SubscribedBlockDto>(
                State.MessageStreamId, AElfIndexerApplicationConsts.MessageStreamNamespace);
        }

        await base.OnActivateAsync();
    }
}