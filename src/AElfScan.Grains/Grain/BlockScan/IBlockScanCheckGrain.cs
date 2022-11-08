using Orleans;

namespace AElfScan.Grains.Grain.BlockScan;

public interface IBlockScanCheckGrain : IGrainWithStringKey, IRemindable
{
    Task Start();
    Task Stop();
}