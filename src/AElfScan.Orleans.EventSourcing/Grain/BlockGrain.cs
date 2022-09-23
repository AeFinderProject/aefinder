using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using AElfScan.EventData;
using AElfScan.State;
using Orleans.EventSourcing;
using Volo.Abp.DependencyInjection;
using Orleans.EventSourcing.Snapshot;
using Orleans.Providers;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan.Grain;

// [StorageProvider(ProviderName = "OrleansStorage")]
// [LogConsistencyProvider(ProviderName = "LogStorage")]
public class BlockGrain:JournaledSnapshotGrain<BlockState>,IBlockGrain
{
    // private readonly ILogger<BlockGrain> _logger;
    //
    // public BlockGrain(
    //     ILogger<BlockGrain> logger)
    // {
    //     _logger = logger;
    // }

    public async Task<List<Block>> SaveBlock(BlockEventData blockEvent)
    {
        //Ignore blocks with height less than LIB block in Dictionary
        foreach (var block in this.State.Blocks)
        {
            if (block.Value.IsConfirmed && blockEvent.BlockNumber <= block.Value.BlockNumber)
            {
                return null;
            }
        }

        // _logger.LogInformation("Start Raise Event of Block Number:" + blockEvent.BlockNumber);
        RaiseEvent(blockEvent);
        // RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0 ? true : false);
        await ConfirmEvents();
        Console.WriteLine("Event has been comfirmed! eventtime:" + blockEvent.BlockTime);

        return this.State.LibBlockList;
    }
}