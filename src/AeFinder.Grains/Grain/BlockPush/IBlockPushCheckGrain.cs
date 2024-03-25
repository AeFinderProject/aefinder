using Orleans;

namespace AeFinder.Grains.Grain.BlockPush;

public interface IBlockPushCheckGrain : IGrainWithStringKey, IRemindable
{
    Task Start();
    Task Stop();
}