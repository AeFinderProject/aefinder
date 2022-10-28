using AElfScan.AElf;
using AElfScan.AElf.Dtos;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using AElfScan.Orleans.EventSourcing.State.ScanClients;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;

namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public class BlockScanGrain : Grain<BlockScanState>, IBlockScanGrain
{
    private readonly IBlockAppService _blockAppService;
    private readonly BlockScanOptions _blockScanOptions;

    private IAsyncStream<SubscribedBlockDto> _stream = null!;

    public BlockScanGrain(IOptionsSnapshot<BlockScanOptions> blockScanOptions, IBlockAppService blockAppService)
    {
        _blockAppService = blockAppService;
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
            
            if (State.ScannedConfirmedBlockHeight >= chainStatus.ConfirmedBlockHeight - _blockScanOptions.ScanHistoryBlockThreshold)
            {
                await clientGrain.SetScanNewBlockStartHeightAsync(State.ScannedConfirmedBlockHeight + 1);
                break;
            }

            var targetHeight = Math.Min(State.ScannedConfirmedBlockHeight + _blockScanOptions.BatchPushBlockCount,
                chainStatus.ConfirmedBlockHeight- _blockScanOptions.ScanHistoryBlockThreshold);
            var blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
            {
                ChainId = State.ChainId,
                HasTransaction = true,
                StartBlockNumber = State.ScannedConfirmedBlockHeight + 1,
                EndBlockNumber = targetHeight,
                Contracts = subscribeInfo.SubscribeEvents
            });

            if (blocks.Count > 0)
            {
                if (!subscribeInfo.OnlyConfirmedBlock)
                {
                    foreach (var block in blocks)
                    {
                        block.IsConfirmed = false;
                    }
                    
                    await _stream.OnNextAsync(new SubscribedBlockDto
                    {
                        ClientId = State.ClientId,
                        ChainId = State.ChainId,
                        Version = State.Version,
                        Blocks = blocks
                    });
                }

                foreach (var block in blocks)
                {
                    block.IsConfirmed = true;
                }
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

        var blocks = new List<BlockDto>();
        if (block.BlockNumber == State.ScannedBlockHeight+1 && block.PreviousBlockHash == State.ScannedBlockHash)
        {
            blocks.Add(block);
        }
        else if(State.ScannedBlocks.Count == 0)
        {
            blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
            {
                ChainId = State.ChainId,
                HasTransaction = true,
                StartBlockNumber = State.ScannedBlockHeight + 1,
                EndBlockNumber = block.BlockNumber
            });
        }
        else if(State.ScannedBlocks.TryGetValue(block.BlockNumber-1, out var previousBlocks) && previousBlocks.Contains(block.PreviousBlockHash))
        {
            blocks.Add(block);
        }
        else
        {
            blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
            {
                ChainId = State.ChainId,
                HasTransaction = true,
                StartBlockNumber = State.ScannedBlocks.Keys.Min(),
                EndBlockNumber = block.BlockNumber
            });
        }
        
        foreach (var b in blocks)
        {
            if (!State.ScannedBlocks.TryGetValue(b.BlockNumber, out var scannedBlocks))
            {
                scannedBlocks = new HashSet<string>();
            }

            scannedBlocks.Add(b.BlockHash);
            State.ScannedBlocks[b.BlockNumber] = scannedBlocks;
        }
        
        State.ScannedBlockHeight = blocks.Last().BlockNumber;
        State.ScannedBlockHash = blocks.Last().BlockHash;

        var subscribedBlocks = FilterBlocks(blocks, subscribeInfo);
        foreach (var subscribedBlock in subscribedBlocks)
        {
            subscribedBlock.IsConfirmed = false;
        }
        
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
            || blocks.First().BlockNumber <= State.ScannedConfirmedBlockHeight)
        {
            return;
        }

        var scannedBlocks = new List<BlockDto>();

        if (blocks.First().BlockNumber == State.ScannedConfirmedBlockHeight + 1)
        {
            scannedBlocks.AddRange(blocks);
        }
        else
        {
            scannedBlocks.AddRange(await _blockAppService.GetBlocksAsync(new GetBlocksInput
            {
                ChainId = State.ChainId,
                HasTransaction = true,
                IsOnlyConfirmed = true,
                StartBlockNumber = State.ScannedConfirmedBlockHeight + 1,
                EndBlockNumber = blocks.Last().BlockNumber
            }));
        }

        var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
        if (!subscribeInfo.OnlyConfirmedBlock)
        {
            foreach (var b in scannedBlocks)
            {
                if (!State.ScannedBlocks.TryGetValue(b.BlockNumber, out var existBlocks) ||
                    !existBlocks.Contains(b.BlockHash))
                {
                    if (b.BlockNumber < State.ScannedBlockHeight)
                    {
                        State.ScannedBlockHeight = b.BlockNumber-1;
                        State.ScannedBlockHash = b.PreviousBlockHash;
                        await WriteStateAsync();
                    }
                    
                    return;
                }
                else
                {
                    State.ScannedBlocks.RemoveAll(o => o.Key <= b.BlockNumber);
                    State.ScannedBlockHeight = b.BlockNumber;
                }
            }
        }

        State.ScannedConfirmedBlockHeight = scannedBlocks.Last().BlockNumber;
        State.ScannedConfirmedBlockHash = scannedBlocks.Last().BlockHash;

        var subscribedBlocks = FilterBlocks(scannedBlocks, subscribeInfo);
        foreach (var block in subscribedBlocks)
        {
            block.IsConfirmed = true;
        }
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            ChainId = State.ChainId,
            Version = State.Version,
            Blocks = subscribedBlocks
        });

        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        
        var streamProvider = GetStreamProvider(AElfScanApplicationConsts.MessageStreamName);

        _stream = streamProvider.GetStream<SubscribedBlockDto>(
            Guid.NewGuid(), AElfScanApplicationConsts.MessageStreamNamespace);
        
        await base.OnActivateAsync();
    }

    private List<BlockDto> FilterBlocks(List<BlockDto> blocks, SubscribeInfo subscribeInfo)
    {
        if (subscribeInfo.SubscribeEvents.Count == 0)
        {
            return blocks;
        }

        var subscribeEvents = new HashSet<string>();
        foreach (var subscribeEvent in subscribeInfo.SubscribeEvents)
        {
            foreach (var eventName in subscribeEvent.EventNames)
            {
                subscribeEvents.Add(subscribeEvent.ContractAddress + eventName);
            }
        }

        return blocks.Where(block => block.Transactions.SelectMany(transaction => transaction.LogEvents).Any(logEvent =>
            subscribeEvents.Contains(logEvent.ContractAddress + logEvent.EventName))).ToList();
    }
}