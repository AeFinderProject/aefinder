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
    
    public async Task NewEvent(BlockEventData @event)
    {
        RaiseEvent(@event);
        await ConfirmEvents();
        Console.WriteLine("Event has been comfirmed! eventtime:" + @event.BlockTime);
    }
}