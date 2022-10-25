using AElfScan.Orleans.EventSourcing.Grain.Chains;
using AElfScan.Orleans.EventSourcing.State.ScanClients;
using Orleans;
using Orleans.Streams;

namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

public class BlockScanGrain : Grain<BlockScanState>, IBlockScanGrain
{
    private readonly IBlockProvider _blockProvider;
    private const long MaxPublishBlockCount = 500;
    private const long ScanHistoryBlockThreshold = 50;
    
    private IAsyncStream<SubscribedBlockDto> _stream = null!;

    public BlockScanGrain(IBlockProvider blockProvider)
    {
        _blockProvider = blockProvider;
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
        if (State.ScannedBlockHeight == 0 && State.ScannedConfirmedBlockHeight == 0)
        {
            State.ScannedBlockHeight = subscribeInfo.StartBlockNumber;
            State.ScannedConfirmedBlockHeight = subscribeInfo.StartBlockNumber;
        }

        while (true)
        {
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.Version != State.Version || clientInfo.ScanModeInfo.ScanMode != ScanMode.HistoricalBlock)
            {
                break;
            }

            // Get Blocks
            var blocks =
                await _blockProvider.GetBlockAsync(State.ScannedConfirmedBlockHeight + 1, State.ScannedConfirmedBlockHeight + MaxPublishBlockCount);

            if (blocks == null || blocks.Count == 0)
            {
                break;
            }

            if (!subscribeInfo.OnlyConfirmedBlock)
            {
                await _stream.OnNextAsync(new SubscribedBlockDto
                {
                    ClientId = State.ClientId,
                    Blocks = blocks
                });
                State.ScannedBlockHeight = blocks.Last().BlockHeight;
            }
            
            await _stream.OnNextAsync(new SubscribedBlockDto
            {
                ClientId = State.ClientId,
                Blocks = blocks
            });
            
            State.ScannedConfirmedBlockHeight = blocks.Last().BlockHeight;
            await WriteStateAsync();

            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (State.ScannedConfirmedBlockHeight > chainStatus.ConfirmBlockHeight - ScanHistoryBlockThreshold)
            {
                await clientGrain.SetScanNewBlockStartHeightAsync(State.ScannedBlockHeight + 1);
                break;
            }
        }
    }

    public async Task HandleNewBlockAsync(Block block)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var clientInfo = await clientGrain.GetClientInfoAsync();
        if (clientInfo.Version != State.Version 
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || block.BlockHeight < State.ScannedBlockHeight)
        {
            return;
        }

        var blocks = new List<Block>();
        if (block.BlockHeight == State.ScannedBlockHeight+1)
        {
            blocks.Add(block);
        }
        else if(State.ScannedBlocks.Count == 0)
        {
            blocks = await _blockProvider.GetBlockAsync(State.ScannedBlockHeight + 1, block.BlockHeight);
        }
        else if(State.ScannedBlocks.TryGetValue(block.BlockHeight-1, out var previousBlocks) && previousBlocks.Contains(block.PreviousBlockHash))
        {
            blocks.Add(block);
        }
        else
        {
            blocks = await _blockProvider.GetBlockAsync(State.ScannedBlockHeight + 1, block.BlockHeight);
        }
        
        foreach (var b in blocks)
        {
            if (!State.ScannedBlocks.TryGetValue(b.BlockHeight, out var scannedBlocks))
            {
                scannedBlocks = new HashSet<string>();
            }

            scannedBlocks.Add(b.BlockHash);
        }

        // Filer block by SubscribeInfo
        var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
        
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            Blocks = blocks
        });

        await WriteStateAsync();
    }
    
    public async Task HandleConfirmedBlockAsync(Block block)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var clientInfo = await clientGrain.GetClientInfoAsync();
        if (clientInfo.Version != State.Version 
            || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock
            || block.BlockHeight < State.ScannedConfirmedBlockHeight)
        {
            return;
        }

        var blocks = new List<Block>();
        if (block.BlockHeight == State.ScannedConfirmedBlockHeight + 1)
        {
            blocks.Add(block);
        }
        else
        {
            // Get Blocks
            blocks = await _blockProvider.GetBlockAsync(State.ScannedBlockHeight + 1, block.BlockHeight);
        }

        foreach (var b in blocks)
        {
            if (!State.ScannedBlocks.TryGetValue(b.BlockHeight, out var scannedBlocks) ||
                !scannedBlocks.Contains(b.BlockHash))
            {
                State.ScannedBlockHeight = Math.Min(State.ScannedBlockHeight, b.BlockHeight - 1);
                await WriteStateAsync();
                return;
            }
            else
            {
                State.ScannedBlocks.RemoveAll(o => o.Key <= b.BlockHeight);
                State.ScannedBlockHeight = b.BlockHeight;
            }
        }
        State.ScannedConfirmedBlockHeight = blocks.Last().BlockHeight;

        // Filer block by SubscribeInfo
        var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
       
        await _stream.OnNextAsync(new SubscribedBlockDto
        {
            ClientId = State.ClientId,
            Blocks = blocks
        });
        
        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        
        var streamProvider = GetStreamProvider("AElfScan");

        _stream = streamProvider.GetStream<SubscribedBlockDto>(
            Guid.NewGuid(), "default");
        
        await base.OnActivateAsync();
    }
}