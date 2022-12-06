using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IBlockScanCheckGrain : IGrainWithStringKey, IRemindable
{
    Task Start();
    Task Stop();
}