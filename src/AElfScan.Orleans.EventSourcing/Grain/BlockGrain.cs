using Orleans.EventSourcing;
using Orleans.Providers;
using System.Threading.Tasks;
using AElfScan.EventData;
using AElfScan.State;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Grain;

[StorageProvider(ProviderName = "OrleansStorage")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class BlockGrain:JournaledGrain<BlockState,BlockEventData>,IBlockGrain
{
    public Task<int> GetBlockCount()
    {
        return Task.FromResult(this.State.BlockCount);
    }
    
    public async Task<bool> NewEvent(BlockEventData @event)
    {
        if (this.State.MaxBlockNumber >= @event.BlockNumber)
        {
            Console.WriteLine("Event can't be raised,because its block number("
                              +@event.BlockNumber +") smaller than database's MaxBlockNumber"
                              +this.State.MaxBlockNumber);
            return false;
        }
        RaiseEvent(@event);
        await ConfirmEvents();
        Console.WriteLine("Event has been comfirmed! eventtime:" + @event.BlockTime);
        return true;
    }

    public async Task<bool> SaveBlock(BlockEventData blockEvent)
    {
        RaiseEvent(blockEvent);
        await ConfirmEvents();
        Console.WriteLine("Event has been comfirmed! eventtime:" + blockEvent.BlockTime);
        return true;
    }
}