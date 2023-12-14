using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public interface IBlockScanCheckGrain : IGrainWithStringKey, IRemindable
{
    Task Start();
    Task Stop();
}