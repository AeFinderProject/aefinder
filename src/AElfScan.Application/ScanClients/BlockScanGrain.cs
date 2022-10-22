using System.Linq;
using System.Threading.Tasks;
using AElfScan.Chains;
using Orleans;

namespace AElfScan.ScanClients;

public class BlockScanGrain : Grain<BlockScanState>, IBlockScanGrain
{
    private readonly IBlockProvider _blockProvider;

    public BlockScanGrain(IBlockProvider blockProvider)
    {
        _blockProvider = blockProvider;
    }

    public async Task InitAsync(string chainId, string clientId, string version)
    {
        State.Version = version;
        State.ClientId = clientId;
        State.ChainId = chainId;
        await WriteStateAsync();
    }
    
    public async Task ScanBlockAsync()
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var chainGrain = GrainFactory.GetGrain<IChainGrain>(State.ChainId);
        var subscribeInfo = await clientGrain.GetSubscribeInfoAsync();
        while (true)
        {
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.Version != State.Version)
            {
                break;
            }

            if (clientInfo.ScanModeInfo.ScanMode != ScanMode.HistoryBlock)
            {
                break;
            }

            // Get Blocks
            var blocks = await _blockProvider.GetBlockAsync(State.ScanHeight+1, State.ScanHeight+1 + 500);
            // Publish Event
            //
            
            State.ScanHeight = blocks.Last().BlockHeight;
            await WriteStateAsync();

            var chainStatus = await chainGrain.GetChainStatusAsync();
            if (State.ScanHeight > chainStatus.BlockHeight + 50)
            {
                await clientGrain.SetScanNewBlockStartHeightAsync(State.ScanHeight + 1);
                break;
            }

        }
    }
    
    public async Task ScanNewBlockAsync(Block block)
    {
        var clientGrain = GrainFactory.GetGrain<IClientGrain>(this.GetPrimaryKeyString());
        var clientInfo = await clientGrain.GetClientInfoAsync();
        if (clientInfo.Version != State.Version)
        {
            return;
        }

        if (clientInfo.ScanModeInfo.ScanMode != ScanMode.NewBlock)
        {
            return;
        }

        if (block.BlockHeight == State.ScanHeight + 1)
        {
            // Publish Event
            //
        }
        else
        {
            // Get Blocks
            var blocks = await _blockProvider.GetBlockAsync(State.ScanHeight+1, block.BlockHeight);
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