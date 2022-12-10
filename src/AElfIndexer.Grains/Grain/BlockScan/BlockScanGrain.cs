using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Grains.State.BlockScan;
using AElfIndexer.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockScanGrain : Grain<BlockScanState>, IBlockScanGrain
{
    private readonly IEnumerable<IBlockFilterProvider> _blockFilterProviders;
    private readonly BlockScanOptions _blockScanOptions;

    private IAsyncStream<SubscribedBlockDto> _stream = null!;

    public BlockScanGrain(IOptionsSnapshot<BlockScanOptions> blockScanOptions,
        IEnumerable<IBlockFilterProvider> blockFilterProviders)
    {
        _blockFilterProviders = blockFilterProviders;
        _blockScanOptions = blockScanOptions.Value;
    }

    public async Task<Guid> InitializeAsync(string chainId, string clientId, string version)
    {
        State.Version = version;
        State.ClientId = clientId;
        State.ChainId = chainId;
        State.ScannedBlockHeight = 0;
        State.ScannedConfirmedBlockHeight = 0;
        State.ScannedBlocks = new SortedDictionary<long, HashSet<string>>();
        await WriteStateAsync();

        return _stream.Guid;
    }

    public async Task HandleHistoricalBlockAsync()
    {
        try
        {
            var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
            var chainGrain = GrainFactory.GetGrain<IChainGrain>(State.ChainId);
            var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (State.ScannedBlockHeight == 0 && State.ScannedConfirmedBlockHeight == 0)
            {
                State.ScannedBlockHeight = subscribeInfo.StartBlockNumber - 1;
                State.ScannedConfirmedBlockHeight = subscribeInfo.StartBlockNumber - 1;
                await WriteStateAsync();
            }

            while (true)
            {
                var clientInfo = await clientGrain.GetClientInfoAsync();
                if (clientInfo.Version != State.Version || clientInfo.ScanModeInfo.ScanMode != ScanMode.HistoricalBlock)
                {
                    break;
                }

                await clientGrain.SetHandleHistoricalBlockTimeAsync(DateTime.UtcNow);

                if (State.ScannedConfirmedBlockHeight >=
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold)
                {
                    await clientGrain.SetScanNewBlockStartHeightAsync(State.ScannedConfirmedBlockHeight + 1);
                    break;
                }

                var targetHeight = Math.Min(State.ScannedConfirmedBlockHeight + _blockScanOptions.BatchPushBlockCount,
                    chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold);


                var blocks = await _blockFilterProviders.First(o => o.FilterType == subscribeInfo.FilterType)
                    .GetBlocksAsync(State.ChainId, State.ScannedConfirmedBlockHeight + 1, targetHeight, false,
                        subscribeInfo.SubscribeEvents);

                if (blocks.Count > 0)
                {
                    if (!subscribeInfo.OnlyConfirmedBlock)
                    {
                        SetIsConfirmed(blocks, false);
                        await _stream.OnNextAsync(new SubscribedBlockDto
                        {
                            ClientId = State.ClientId,
                            ChainId = State.ChainId,
                            Version = State.Version,
                            Blocks = blocks
                        });
                    }

                    SetIsConfirmed(blocks, true);
                    await _stream.OnNextAsync(new SubscribedBlockDto
                    {
                        ClientId = State.ClientId,
                        ChainId = State.ChainId,
                        Version = State.Version,
                        Blocks = blocks
                    });
                }

                State.ScannedBlockHeight = targetHeight;
                State.ScannedConfirmedBlockHeight = targetHeight;

                await WriteStateAsync();

                chainStatus = await chainGrain.GetChainStatusAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"HandleHistoricalBlock failed: {e.Message}");
            throw;
        }
    }

    public async Task HandleNewBlockAsync(BlockDto block)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var clientInfo = await clientGrain.GetClientInfoAsync();
        var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();

        if (clientInfo.Version != State.Version
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || subscribeInfo.OnlyConfirmedBlock)
        {
            return;
        }

        var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == subscribeInfo.FilterType);
        var blocks = new List<BlockDto>();
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
            blocks = await blockFilterProvider.GetBlocksAsync(State.ChainId, State.ScannedBlocks.Keys.Min(),
                block.BlockHeight, false, null);
        }

        var unPushedBlock = new List<BlockDto>();
        foreach (var b in blocks)
        {
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
            Blocks = subscribedBlocks
        });

        await WriteStateAsync();
    }

    public async Task HandleConfirmedBlockAsync(List<BlockDto> blocks)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var clientInfo = await clientGrain.GetClientInfoAsync();
        if (clientInfo.Version != State.Version
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || blocks.First().BlockHeight <= State.ScannedConfirmedBlockHeight)
        {
            return;
        }

        var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
        var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == subscribeInfo.FilterType);

        var scannedBlocks = new List<BlockDto>();
        if (blocks.First().BlockHeight == State.ScannedConfirmedBlockHeight + 1)
        {
            scannedBlocks.AddRange(blocks);
        }
        else
        {
            scannedBlocks.AddRange(await blockFilterProvider.GetBlocksAsync(State.ChainId,
                State.ScannedConfirmedBlockHeight + 1,
                blocks.Last().BlockHeight, true, null));
        }

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

        State.ScannedConfirmedBlockHeight = scannedBlocks.Last().BlockHeight;
        State.ScannedConfirmedBlockHash = scannedBlocks.Last().BlockHash;

        var subscribedBlocks =
            await blockFilterProvider.FilterBlocksAsync(scannedBlocks, subscribeInfo.SubscribeEvents);

        SetIsConfirmed(subscribedBlocks, true);
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            Blocks = subscribedBlocks
        });

        await WriteStateAsync();
    }

    private void SetIsConfirmed(List<BlockDto> blocks, bool isConfirmed)
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

        var streamProvider = GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName);

        _stream = streamProvider.GetStream<SubscribedBlockDto>(
            Guid.NewGuid(), AElfIndexerApplicationConsts.MessageStreamNamespace);

        await base.OnActivateAsync();
    }
}