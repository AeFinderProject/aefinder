using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        var clientGrain = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var steamId = await clientGrain.GetMessageStreamIdAsync();
        State.MessageStreamId = steamId;

        var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);
        _stream = streamProvider.GetStream<SubscribedBlockDto>(
            steamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        await WriteStateAsync();

        return _stream.Guid;
    }

    public async Task HandleHistoricalBlockAsync()
    {
        try
        {
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


                var filteredBlocks = await _blockFilterProviders.First(o => o.FilterType == subscriptionInfo.FilterType)
                    .GetBlocksAsync(State.ChainId, State.ScannedConfirmedBlockHeight + 1, targetHeight, true,
                        subscriptionInfo.SubscribeEvents);
                
                var blocks = await FillVacantBlockAsync(filteredBlocks, State.ScannedConfirmedBlockHeight + 1,
                    targetHeight);

                if (blocks.Count != targetHeight - State.ScannedConfirmedBlockHeight)
                {
                    throw new ApplicationException(
                        $"Cannot fill vacant blocks: from {State.ScannedConfirmedBlockHeight + 1} to {targetHeight}");
                }

                if (!subscriptionInfo.OnlyConfirmedBlock)
                {
                    SetIsConfirmed(blocks, false);
                    await _stream.OnNextAsync(new SubscribedBlockDto
                    {
                        ClientId = State.ClientId,
                        ChainId = State.ChainId,
                        Version = State.Version,
                        FilterType = subscriptionInfo.FilterType,
                        Blocks = blocks
                    });
                }

                SetIsConfirmed(blocks, true);
                await _stream.OnNextAsync(new SubscribedBlockDto
                {
                    ClientId = State.ClientId,
                    ChainId = State.ChainId,
                    Version = State.Version,
                    FilterType = subscriptionInfo.FilterType,
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
        if (filteredBlocks.Count == endHeight- startHeight + 1)
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
        _logger.LogDebug($"HandleNewBlock {this.GetPrimaryKeyString()}, block height: {block.BlockHeight}, block hash: {block.BlockHash}");
        
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var clientInfo = await blockScanInfo.GetClientInfoAsync();
        var subscriptionInfo = await blockScanInfo.GetSubscriptionInfoAsync();

        var isVersionAvailable = await clientGrain.IsVersionAvailableAsync(State.Version);
        if (!isVersionAvailable
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || subscriptionInfo.OnlyConfirmedBlock)
        {
            _logger.LogDebug($"HandleNewBlock failed {this.GetPrimaryKeyString()}, block height: {block.BlockHeight}, block hash: {block.BlockHash}");
            return;
        }

        var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == subscriptionInfo.FilterType);
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
            _logger.LogDebug($"HandleNewBlock failed {this.GetPrimaryKeyString()}, no unpushed block. block height: {block.BlockHeight}, block hash: {block.BlockHash}");
            return;
        }

        State.ScannedBlockHeight = unPushedBlock.Last().BlockHeight;
        State.ScannedBlockHash = unPushedBlock.Last().BlockHash;

        var subscribedBlocks = await blockFilterProvider.FilterBlocksAsync(unPushedBlock, subscriptionInfo.SubscribeEvents);
        
        SetIsConfirmed(subscribedBlocks, false);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            FilterType = subscriptionInfo.FilterType,
            Blocks = subscribedBlocks
        });

        await WriteStateAsync();
    }

    public async Task HandleConfirmedBlockAsync(List<BlockWithTransactionDto> blocks)
    {
        _logger.LogDebug($"HandleConfirmedBlock {this.GetPrimaryKeyString()}, start block height: {blocks.First().BlockHeight}, end block height: {blocks.Last().BlockHeight}");

        var clientGrain = GrainFactory.GetGrain<IClientGrain>(State.ClientId);
        var blockScanInfo = GrainFactory.GetGrain<IBlockScanInfoGrain>(this.GetPrimaryKeyString());
        var clientInfo = await blockScanInfo.GetClientInfoAsync();
        
        var isVersionAvailable = await clientGrain.IsVersionAvailableAsync(State.Version);
        if (!isVersionAvailable
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || blocks.First().BlockHeight <= State.ScannedConfirmedBlockHeight)
        {
            _logger.LogDebug($"HandleConfirmedBlock failed {this.GetPrimaryKeyString()}, start block height: {blocks.First().BlockHeight}, end block height: {blocks.Last().BlockHeight}");
            return;
        }

        var subscriptionInfo = await blockScanInfo.GetSubscriptionInfoAsync();
        var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == subscriptionInfo.FilterType);

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
        
        var subscribedBlocks =
            await blockFilterProvider.FilterBlocksAsync(scannedBlocks, subscriptionInfo.SubscribeEvents);

        subscribedBlocks = await blockFilterProvider.FilterIncompleteConfirmedBlocksAsync(State.ChainId, subscribedBlocks,
            State.ScannedConfirmedBlockHash, State.ScannedConfirmedBlockHeight);

        if (!subscriptionInfo.OnlyConfirmedBlock)
        {
            foreach (var b in subscribedBlocks)
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
        
        SetIsConfirmed(subscribedBlocks, true);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            FilterType = subscriptionInfo.FilterType,
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
            block.Confirmed = isConfirmed;
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