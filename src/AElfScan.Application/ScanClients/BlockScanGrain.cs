using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.Chains;
using Orleans;

namespace AElfScan.ScanClients;

public class BlockScanGrain : Grain<BlockScanState>, IBlockScanGrain
{
    private readonly IBlockProvider _blockProvider;
    private const long MaxPublishBlockCount = 50;
    private const long ScanHistoryBlockThreshold = 5;

    public BlockScanGrain(IBlockProvider blockProvider)
    {
        _blockProvider = blockProvider;
    }

    public async Task InitializeAsync(string chainId, string clientId, string version)
    {
        State.Version = version;
        State.ClientId = clientId;
        State.ChainId = chainId;
        State.ScanHeight = 0;
        await WriteStateAsync();
    }

    public async Task HandleHistoricalBlockAsync()
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var chainGrain = GrainFactory.GetGrain<IChainGrain>(State.ChainId);
        var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
        if (State.ScanHeight == 0)
        {
            State.ScanHeight = subscribeInfo.StartBlockNumber;
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
                await _blockProvider.GetBlockAsync(State.ScanHeight + 1, State.ScanHeight + 1 + MaxPublishBlockCount);

            if (blocks == null || blocks.Count == 0)
            {
                break;
            }
            
            // Publish Event
            // 

            State.ScanHeight = blocks.Last().BlockHeight;
            await WriteStateAsync();

            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (State.ScanHeight > chainStatus.BlockHeight - ScanHistoryBlockThreshold)
            {
                await clientGrain.SetScanNewBlockStartHeightAsync(State.ScanHeight + 1);
                break;
            }
        }
    }

    public async Task HandleNewBlockAsync(Block block)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var clientInfo = await clientGrain.GetClientInfoAsync();
        if (clientInfo.Version != State.Version || clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock)
        {
            return;
        }

        var blocks = new List<Block>();
        if (block.BlockHeight == State.ScanHeight + 1)
        {
            // Filer block by SubscribeInfo
            var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
            blocks.Add(block);
        }
        else
        {
            // Get Blocks
            blocks = await _blockProvider.GetBlockAsync(State.ScanHeight + 1, block.BlockHeight);
        }

        if (blocks.Count == 0)
        {
            return;
        }
        
        // Publish Event
        //
        
        State.ScanHeight = block.BlockHeight;
        await WriteStateAsync();
    }

    public override Task OnActivateAsync()
    {
        this.ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        this.WriteStateAsync();
        return base.OnDeactivateAsync();
    }
}