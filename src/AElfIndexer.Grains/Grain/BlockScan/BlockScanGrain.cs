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
    private readonly IEnumerable<IBlockFilterProvider> _blockFilterProviders;
    private readonly BlockScanOptions _blockScanOptions;
    private readonly ILogger<BlockScanGrain> _logger;

    private IAsyncStream<SubscribedBlockDto> _stream = null!;

    public BlockScanGrain(IOptionsSnapshot<BlockScanOptions> blockScanOptions,
        IEnumerable<IBlockFilterProvider> blockFilterProviders, ILogger<BlockScanGrain> logger)
    {
        _blockFilterProviders = blockFilterProviders;
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
        var steamId = await clientGrain.GetMessageStreamIdAsync();
        State.MessageStreamId = steamId;
        
        await WriteStateAsync();

        if (_stream == null)
        {
            var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);
        
            _stream = streamProvider.GetStream<SubscribedBlockDto>(
                Guid.NewGuid(), AElfIndexerApplicationConsts.MessageStreamNamespace);
        }

        return _stream.Guid;
    }

    public async Task HandleHistoricalBlockAsync()
    {
        try
        {
            var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
            var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
            var chainGrain = GrainFactory.GetGrain<IChainGrain>(State.ChainId);
            var subscribeInfo = await blockScanInfo.GetSubscribeInfoAsync();
            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (State.ScannedBlockHeight == 0 && State.ScannedConfirmedBlockHeight == 0)
            {
                State.ScannedBlockHeight = subscribeInfo.StartBlockNumber - 1;
                State.ScannedConfirmedBlockHeight = subscribeInfo.StartBlockNumber - 1;
                await WriteStateAsync();
            }

            while (true)
            {
                var clientInfo = await blockScanInfo.GetClientInfoAsync();
                var isVersionAvailable = await clientGrain.IsVersionAvailableAsync(State.Version);
                if (!isVersionAvailable || clientInfo.ScanModeInfo.ScanMode != ScanMode.HistoricalBlock)
                {
                    break;
                }

                await blockScanInfo.SetHandleHistoricalBlockTimeAsync(DateTime.UtcNow);

                if (State.ScannedConfirmedBlockHeight >=
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold)
                {
                    await blockScanInfo.SetScanNewBlockStartHeightAsync(State.ScannedConfirmedBlockHeight + 1);
                    break;
                }

                var targetHeight = Math.Min(State.ScannedConfirmedBlockHeight + _blockScanOptions.BatchPushBlockCount,
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold);


                var filteredBlocks = await _blockFilterProviders.First(o => o.FilterType == subscribeInfo.FilterType)
                    .GetBlocksAsync(State.ChainId, State.ScannedConfirmedBlockHeight + 1, targetHeight, true,
                        subscribeInfo.SubscribeEvents);

                var blocks = await FillVacantBlockAsync(filteredBlocks, State.ScannedConfirmedBlockHeight + 1,
                    targetHeight);

                if (!subscribeInfo.OnlyConfirmedBlock)
                {
                    SetIsConfirmed(blocks, false);
                    await _stream.OnNextAsync(new SubscribedBlockDto
                    {
                        ClientId = State.ClientId,
                        ChainId = State.ChainId,
                        Version = State.Version,
                        FilterType = subscribeInfo.FilterType,
                        Blocks = blocks
                    });
                }

                SetIsConfirmed(blocks, true);
                await _stream.OnNextAsync(new SubscribedBlockDto
                {
                    ClientId = State.ClientId,
                    ChainId = State.ChainId,
                    Version = State.Version,
                    FilterType = subscribeInfo.FilterType,
                    Blocks = blocks
                });

                State.ScannedBlockHeight = blocks.Last().BlockHeight;
                State.ScannedBlockHash = blocks.Last().BlockHash;
                State.ScannedConfirmedBlockHeight = blocks.Last().BlockHeight;;
                State.ScannedConfirmedBlockHash = blocks.Last().BlockHash;

                await WriteStateAsync();

                chainStatus = await chainGrain.GetChainStatusAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"HandleHistoricalBlock failed: {e.Message}");
            throw;
        }
    }

    private async Task<List<BlockWithTransactionDto>> FillVacantBlockAsync(List<BlockWithTransactionDto> filteredBlocks, long startHeight,
        long endHeight)
    {
        if (filteredBlocks.Count == startHeight - endHeight + 1)
        {
            return filteredBlocks;
        }

        var result = new List<BlockWithTransactionDto>();
        var allBlocks = await _blockFilterProviders.First(o => o.FilterType == BlockFilterType.Block)
            .GetBlocksAsync(State.ChainId, startHeight, endHeight, true, null);
        var filteredBlockDic = filteredBlocks.ToDictionary(o => o.BlockHeight, o => o);
        foreach (var b in allBlocks)
        {
            if (filteredBlockDic.TryGetValue(b.BlockHeight, out var filteredBlock))
            {
                result.Add(filteredBlock);
            }
            else
            {
                result.Add(b);
            }
        }

        return result;
    }

    public async Task HandleNewBlockAsync(BlockWithTransactionDto block)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var clientInfo = await blockScanInfo.GetClientInfoAsync();
        var subscribeInfo = await blockScanInfo.GetSubscribeInfoAsync();

        var isVersionAvailable = await clientGrain.IsVersionAvailableAsync(State.Version);
        if (!isVersionAvailable
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || subscribeInfo.OnlyConfirmedBlock)
        {
            return;
        }

        var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == subscribeInfo.FilterType);
        var blocks = new List<BlockWithTransactionDto>();
        if (block.BlockHeight == State.ScannedBlockHeight + 1 && block.PreviousBlockHash == State.ScannedBlockHash)
        {
            blocks.Add(block);
        }
        else if (State.ScannedBlocks.Count == 0)
        {
            blocks = await blockFilterProvider.GetBlocksAsync(State.ChainId, State.ScannedBlockHeight + 1,
                block.BlockHeight, false, null);
        }
        else if (State.ScannedBlocks.TryGetValue(block.BlockHeight - 1, out var previousBlocks) &&
                 previousBlocks.Contains(block.PreviousBlockHash))
        {
            blocks.Add(block);
        }
        else
        {
            _logger.LogDebug($"Not linked new block, block height: {block.BlockHeight}, block hash: {block.BlockHash}");
            blocks = await blockFilterProvider.GetBlocksAsync(State.ChainId, GetMinScannedBlockHeight(),
                block.BlockHeight, false, null);
        }

        blocks = await blockFilterProvider.FilterIncompleteBlocksAsync(State.ChainId, blocks);

        var unPushedBlock = GetUnPushedBlocks(blocks);
        if (unPushedBlock.Count == 0)
        {
            return;
        }

        State.ScannedBlockHeight = unPushedBlock.Last().BlockHeight;
        State.ScannedBlockHash = unPushedBlock.Last().BlockHash;

        var subscribedBlocks = await blockFilterProvider.FilterBlocksAsync(unPushedBlock, subscribeInfo.SubscribeEvents);
        
        SetIsConfirmed(subscribedBlocks, false);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            FilterType = subscribeInfo.FilterType,
            Blocks = subscribedBlocks
        });

        await WriteStateAsync();
    }

    public async Task HandleConfirmedBlockAsync(List<BlockWithTransactionDto> blocks)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var clientInfo = await blockScanInfo.GetClientInfoAsync();
        
        var isVersionAvailable = await clientGrain.IsVersionAvailableAsync(State.Version);
        if (!isVersionAvailable
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || blocks.First().BlockHeight <= State.ScannedConfirmedBlockHeight)
        {
            return;
        }

        var subscribeInfo = await blockScanInfo.GetSubscribeInfoAsync();
        var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == subscribeInfo.FilterType);

        var scannedBlocks = new List<BlockWithTransactionDto>();
        if (blocks.First().BlockHeight == State.ScannedConfirmedBlockHeight + 1)
        {
            scannedBlocks.AddRange(blocks);
        }
        else
        {
            _logger.LogDebug($"Not linked confirmed block, block height: {blocks.First().BlockHeight}, block hash: {blocks.First().BlockHash}");
            scannedBlocks.AddRange(await blockFilterProvider.GetBlocksAsync(State.ChainId,
                State.ScannedConfirmedBlockHeight + 1,
                blocks.Last().BlockHeight, true, null));
        }

        scannedBlocks = await blockFilterProvider.FilterIncompleteConfirmedBlocksAsync(State.ChainId, scannedBlocks,
            State.ScannedConfirmedBlockHash, State.ScannedConfirmedBlockHeight);

        if (!subscribeInfo.OnlyConfirmedBlock)
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

        var subscribedBlocks =
            await blockFilterProvider.FilterBlocksAsync(scannedBlocks, subscribeInfo.SubscribeEvents);

        SetIsConfirmed(subscribedBlocks, true);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            FilterType = subscribeInfo.FilterType,
            Blocks = subscribedBlocks
        });

        State.ScannedConfirmedBlockHeight = scannedBlocks.Last().BlockHeight;
        State.ScannedConfirmedBlockHash = scannedBlocks.Last().BlockHash;
        await WriteStateAsync();
    }

    private List<BlockWithTransactionDto> GetUnPushedBlocks(List<BlockWithTransactionDto> blocks)
    {
        var unPushedBlock = new List<BlockWithTransactionDto>();
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

    private void SetIsConfirmed(List<BlockWithTransactionDto> blocks, bool isConfirmed)
    {
        foreach (var block in blocks)
        {
            block.IsConfirmed = isConfirmed;
            foreach (var transaction in block.Transactions)
            {
                transaction.IsConfirmed = isConfirmed;
                foreach (var logEvent in transaction.LogEvents)
                {
                    logEvent.IsConfirmed = isConfirmed;
                }
            }
        }
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();

        if (State.MessageStreamId != Guid.Empty)
        {
            var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);

            _stream = streamProvider.GetStream<SubscribedBlockDto>(
                Guid.NewGuid(), AElfIndexerApplicationConsts.MessageStreamNamespace);
        }

        await base.OnActivateAsync();
    }
}