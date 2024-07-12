using Orleans;
using Orleans.Runtime;

namespace AeFinder.Grains.Grain.BlockPush;

public class BlockPushCheckGrain : global::Orleans.Grain, IBlockPushCheckGrain
{
    private IGrainReminder _reminder = null;

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        // TODO: Use IManagementGrain to check the Grain status after the 4.0 release
        // https://github.com/dotnet/orleans/pull/7216

        var clientManagerGrain = GrainFactory.GetGrain<IBlockPusherManagerGrain>(GrainIdHelper.GenerateBlockPusherManagerGrainId());
        var ids = await clientManagerGrain.GetAllBlockPusherIdsAsync();
        foreach (var (_, set) in ids)
        {
            foreach (var id in set)
            {
                var clientGrain = GrainFactory.GetGrain<IBlockPusherInfoGrain>(id);
                if (!await clientGrain.IsNeedRecoverAsync())
                {
                    continue;
                }

                var blockPusherGrain = GrainFactory.GetGrain<IBlockPusherGrain>(id);
                _ = Task.Run(blockPusherGrain.HandleHistoricalBlockAsync);
            }
        }
    }

    public async Task Start()
    {
        if (_reminder != null)
        {
            return;
        }
        _reminder = await this.RegisterOrUpdateReminder(
            this.GetPrimaryKeyString(),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromMinutes(1) 
        );
    }
    public async Task Stop()
    {
        if (_reminder == null)
        {
            return;
        }
        await this.UnregisterReminder(_reminder);
        _reminder = null;
    }
}