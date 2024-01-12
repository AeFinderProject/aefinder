using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Grains.Grain.ScanApps;
using AElfIndexer.Grains.State.BlockScanExecution;
using AElfIndexer.Grains.State.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public class BlockScanExecutorGrain : Grain<BlockScanExecutorState>, IBlockScanExecutorGrain
{
    private readonly IBlockFilterProvider _blockFilterProvider;
    private readonly BlockScanOptions _blockScanOptions;
    private readonly ILogger<BlockScanExecutorGrain> _logger;
    private readonly IObjectMapper _objectMapper;

    private IAsyncStream<SubscribedBlockDto> _stream = null!;

    private string _scanAppId;
    private string _subscriptionVersion;
    private SubscriptionItem _subscription;

    public BlockScanExecutorGrain(IOptionsSnapshot<BlockScanOptions> blockScanOptions,
        IBlockFilterProvider blockFilterProvider, ILogger<BlockScanExecutorGrain> logger, IObjectMapper objectMapper)
    {
        _blockFilterProvider = blockFilterProvider;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockScanOptions = blockScanOptions.Value;
    }

    public async Task InitializeAsync(string scanToken, long startHeight)
    {
        State.ScannedBlockHeight = startHeight - 1;
        State.ScannedBlockHash = null;
        State.ScannedConfirmedBlockHeight = startHeight - 1;
        State.ScannedConfirmedBlockHash = null;
        State.ScannedBlocks = new SortedDictionary<long, HashSet<string>>();
        State.ScanToken = scanToken;
        
        var blockScanGrain = GrainFactory.GetGrain<IBlockScanGrain>(this.GetPrimaryKeyString());
        await blockScanGrain.SetHistoricalBlockScanModeAsync();
        
        var scanInfo = await blockScanGrain.GetScanInfoAsync();
        _scanAppId = scanInfo.ScanAppId;
        _subscriptionVersion = scanInfo.Version;
        _subscription = await blockScanGrain.GetSubscriptionAsync();

        await WriteStateAsync();
    }
    
    public async Task HandleHistoricalBlockAsync()
    {
        try
        {
            await ReadStateAsync();

            _logger.LogInformation("Grain: {GrainId} token: {ScanToken} pushing block begin from {ScannedBlockHeight}",
                this.GetPrimaryKeyString(), State.ScanToken, State.ScannedBlockHeight);

            var blockScanGrain = GrainFactory.GetGrain<IBlockScanGrain>(this.GetPrimaryKeyString());
            var scanAppGrain = GrainFactory.GetGrain<IScanAppGrain>(GrainIdHelper.GenerateScanAppGrainId(_scanAppId));
            var chainGrain = GrainFactory.GetGrain<IChainGrain>(_subscription.ChainId);
            var chainStatus = await chainGrain.GetChainStatusAsync();

            while (true)
            {
                if (!await scanAppGrain.IsRunningAsync(_subscriptionVersion, _subscription.ChainId, State.ScanToken) ||
                    await blockScanGrain.GetScanModeAsync() != ScanMode.HistoricalBlock ||
                    !await CheckPushThresholdAsync(_subscription.StartBlockNumber, State.ScannedConfirmedBlockHeight, _blockScanOptions.MaxHistoricalBlockPushThreshold))
                {
                    break;
                }

                await blockScanGrain.SetHandleHistoricalBlockTimeAsync(DateTime.UtcNow);

                if (State.ScannedConfirmedBlockHeight >=
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold)
                {
                    _logger.LogInformation(
                        "Grain: {GrainId} token: {ScanToken} switch to new block mode. ScannedConfirmedBlockHeight: {ScannedConfirmedBlockHeight}, ConfirmedBlockHeight: {ConfirmedBlockHeight}, ScanHistoryBlockThreshold: {ScanHistoryBlockThreshold}",
                        this.GetPrimaryKeyString(), State.ScanToken, State.ScannedConfirmedBlockHeight,
                        chainStatus.ConfirmedBlockHeight, _blockScanOptions.ScanHistoryBlockThreshold);
                    await blockScanGrain.SetScanNewBlockStartHeightAsync(State.ScannedConfirmedBlockHeight + 1);
                    break;
                }

                var targetHeight = Math.Min(State.ScannedConfirmedBlockHeight + _blockScanOptions.BatchPushBlockCount,
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold);


                var blocks = await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
                {
                    ChainId = _subscription.ChainId,
                    StartBlockHeight = State.ScannedConfirmedBlockHeight + 1,
                    EndBlockHeight = targetHeight,
                    IsOnlyConfirmed = true,
                    TransactionFilters = _objectMapper.Map<List<TransactionCondition>, List<FilterTransactionInput>>(_subscription.TransactionConditions),
                    LogEventFilters = _objectMapper.Map<List<LogEventCondition>, List<FilterContractEventInput>>(_subscription.LogEventConditions)
                });

                if (blocks.Count != targetHeight - State.ScannedConfirmedBlockHeight)
                {
                    throw new ApplicationException(
                        $"Cannot fill vacant blocks: from {State.ScannedConfirmedBlockHeight + 1} to {targetHeight}");
                }

                if (!_subscription.OnlyConfirmed)
                {
                    SetConfirmed(blocks, false);
                    await _stream.OnNextAsync(new SubscribedBlockDto
                    {
                        ClientId = _scanAppId,
                        ChainId = _subscription.ChainId,
                        Version = _subscriptionVersion,
                        Blocks = blocks,
                        Token = State.ScanToken
                    });
                }

                SetConfirmed(blocks, true);
                await _stream.OnNextAsync(new SubscribedBlockDto
                {
                    ClientId = _scanAppId,
                    ChainId = _subscription.ChainId,
                    Version = _subscriptionVersion,
                    Blocks = blocks,
                    Token = State.ScanToken
                });

                State.ScannedBlockHeight = blocks.Last().BlockHeight;
                State.ScannedBlockHash = blocks.Last().BlockHash;
                State.ScannedConfirmedBlockHeight = blocks.Last().BlockHeight;;
                State.ScannedConfirmedBlockHash = blocks.Last().BlockHash;

                await WriteStateAsync();

                chainStatus = await chainGrain.GetChainStatusAsync();
                _logger.LogInformation(
                    "Grain: {GrainId} token: {ScanToken} pushed historical block from {BeginBlockHeight} to {EndBlockHeight}",
                    this.GetPrimaryKeyString(), State.ScanToken, blocks.First().BlockHeight, blocks.Last().BlockHeight);

            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Grain: {GrainId} token: {ScanToken} handle historical block failed: {Message}",
                this.GetPrimaryKeyString(), State.ScanToken, e.Message);
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

        var scanAppGrain = GrainFactory.GetGrain<IScanAppGrain>(GrainIdHelper.GenerateScanAppGrainId(_scanAppId));
        var scanInfo = GrainFactory.GetGrain<IBlockScanGrain>(this.GetPrimaryKeyString());

        if (!await CheckPushThresholdAsync(_subscription.StartBlockNumber, State.ScannedBlockHeight, _blockScanOptions.MaxNewBlockPushThreshold))
        {
            return;
        }

        if (_subscription.OnlyConfirmed
            || await scanInfo.GetScanModeAsync() != ScanMode.NewBlock
            || !await scanAppGrain.IsRunningAsync(_subscriptionVersion, _subscription.ChainId, State.ScanToken))
        {
            _logger.LogWarning(
                "Grain: {GrainId} token: {ScanToken} handle block failed, block height: {BlockHeight}, block hash: {BlockHash}",
                this.GetPrimaryKeyString(), State.ScanToken, block.BlockHeight, block.BlockHash);
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
                ChainId = _subscription.ChainId,
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
            _logger.LogDebug(
                "Grain: {GrainId} token: {ScanToken} found not linked block, block height: {BlockHeight}, block hash: {BlockHash}, from height: {startHeight}",
                this.GetPrimaryKeyString(), State.ScanToken, block.BlockHeight, block.BlockHash, startHeight);
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
                $"Grain: {this.GetPrimaryKeyString()} token: {State.ScanToken} cannot get blocks: from {startHeight} to {block.BlockHeight}";
            _logger.LogError(message);
            throw new ApplicationException(message);
        }

        var unPushedBlock = await GetUnPushedBlocksAsync(blocks, needCheckIncomplete);
        if (unPushedBlock.Count == 0)
        {
            _logger.LogWarning(
                "Grain: {GrainId} token: {ScanToken} handle block failed, no unpushed block. block height: {BlockHeight}, block hash: {BlockHash}",
                this.GetPrimaryKeyString(), State.ScanToken, block.BlockHeight, block.BlockHash);
            return;
        }

        State.ScannedBlockHeight = unPushedBlock.Last().BlockHeight;
        State.ScannedBlockHash = unPushedBlock.Last().BlockHash;

        var subscribedBlocks = await _blockFilterProvider.FilterBlocksAsync(unPushedBlock, _subscription.TransactionConditions,
            _subscription.LogEventConditions);
        
        SetConfirmed(subscribedBlocks, false);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = _scanAppId,
            ChainId = _subscription.ChainId,
            Version = _subscriptionVersion,
            Blocks = subscribedBlocks,
            Token = State.ScanToken
        });

        await WriteStateAsync();
        _logger.LogInformation("Grain: {GrainId} token: {ScanToken} pushed block from {First} to {Last}",
            this.GetPrimaryKeyString(), State.ScanToken, subscribedBlocks.First().BlockHeight,
            subscribedBlocks.Last().BlockHeight);
    }

    public async Task HandleConfirmedBlockAsync(BlockWithTransactionDto block)
    {
        await ReadStateAsync();
        
        if (block.BlockHeight < State.ScannedConfirmedBlockHeight + _blockScanOptions.BatchPushNewBlockCount)
        {
            return;
        }
        
        var scanAppGrain = GrainFactory.GetGrain<IScanAppGrain>(GrainIdHelper.GenerateScanAppGrainId(_scanAppId));
        var scanInfo = GrainFactory.GetGrain<IBlockScanGrain>(this.GetPrimaryKeyString());
        
        if (!await CheckPushThresholdAsync(_subscription.StartBlockNumber, State.ScannedConfirmedBlockHeight, _blockScanOptions.MaxNewBlockPushThreshold))
        {
            return;
        }
        
        if (block.BlockHeight <= State.ScannedConfirmedBlockHeight
            || await scanInfo.GetScanModeAsync() != ScanMode.NewBlock
            || !await scanAppGrain.IsRunningAsync(_subscriptionVersion, _subscription.ChainId, State.ScanToken))
        {
            _logger.LogWarning(
                "Grain: {GrainId} token: {ScanToken} handle confirmed block failed, block height: {BlockHeight}",
                this.GetPrimaryKeyString(), State.ScanToken, block.BlockHeight);
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
                "Grain: {GrainId} token: {ScanToken} found not linked confirmed block, block height: {BlockHeight}, block hash: {BlockHash}, current block height: {ScannedConfirmedBlockHeight}",
                this.GetPrimaryKeyString(), State.ScanToken, block.BlockHeight, block.BlockHash,
                State.ScannedConfirmedBlockHeight);

            var startHeight = State.ScannedConfirmedBlockHeight + 1;
            scannedBlocks.AddRange(await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = _subscription.ChainId,
                StartBlockHeight = startHeight,
                EndBlockHeight = GetMaxTargetHeight(startHeight, block.BlockHeight),
                IsOnlyConfirmed = true
            }));

            scannedBlocks = await _blockFilterProvider.FilterIncompleteConfirmedBlocksAsync(_subscription.ChainId,
                scannedBlocks,
                State.ScannedConfirmedBlockHash, State.ScannedConfirmedBlockHeight);
        }

        scannedBlocks =
            await _blockFilterProvider.FilterBlocksAsync(scannedBlocks, _subscription.TransactionConditions,_subscription.LogEventConditions);

        if (scannedBlocks.Count == 0)
        {
            var message =
                $"Grain: {this.GetPrimaryKeyString()} token: {State.ScanToken} cannot get confirmed blocks: from {State.ScannedConfirmedBlockHeight + 1} to {block.BlockHeight}";
            _logger.LogError(message);
            throw new ApplicationException(message);
        }

        if (!_subscription.OnlyConfirmed)
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
            ClientId = _scanAppId,
            ChainId = _subscription.ChainId,
            Version = _subscriptionVersion,
            Blocks = scannedBlocks,
            Token = State.ScanToken
        });

        State.ScannedConfirmedBlockHeight = scannedBlocks.Last().BlockHeight;
        State.ScannedConfirmedBlockHash = scannedBlocks.Last().BlockHash;
        await WriteStateAsync();

        _logger.LogInformation(
            "Grain: {GrainId} token: {ScanToken} pushed confirmed block from {FromBlockHeight} to {ToBlockHeight}",
            this.GetPrimaryKeyString(), State.ScanToken, scannedBlocks.First().BlockHeight,
            scannedBlocks.Last().BlockHeight);
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
                        $"Grain: {this.GetPrimaryKeyString()} token: {State.ScanToken} no block filtered",
                        this.GetPrimaryKeyString(), State.ScanToken);
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
                ChainId = _subscription.ChainId,
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

        var blockScanGrain = GrainFactory.GetGrain<IBlockScanGrain>(this.GetPrimaryKeyString());
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);
        _stream = streamProvider.GetStream<SubscribedBlockDto>(
            streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        await base.OnActivateAsync();
    }
}