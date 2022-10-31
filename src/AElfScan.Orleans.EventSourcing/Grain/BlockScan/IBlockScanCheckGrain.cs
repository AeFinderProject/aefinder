using Orleans;

namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public interface IBlockScanCheckGrain : IGrainWithStringKey, IRemindable
{
    Task Start();
    Task Stop();
}