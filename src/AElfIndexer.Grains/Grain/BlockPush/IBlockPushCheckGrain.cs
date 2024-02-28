using Orleans;

namespace AElfIndexer.Grains.Grain.BlockPush;

public interface IBlockPushCheckGrain : IGrainWithStringKey, IRemindable
{
    Task Start();
    Task Stop();
}