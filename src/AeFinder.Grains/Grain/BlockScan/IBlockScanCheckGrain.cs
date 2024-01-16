using Orleans;

namespace AeFinder.Grains.Grain.BlockScan;

public interface IBlockScanCheckGrain : IGrainWithStringKey, IRemindable
{
    Task Start();
    Task Stop();
}