using Orleans;
using Orleans.Runtime;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public class BlockScanCheckGrain : global::Orleans.Grain, IBlockScanCheckGrain
{
    private IGrainReminder _reminder = null;

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        // TODO: Use IManagementGrain to check the Grain status after the 4.0 release
        // https://github.com/dotnet/orleans/pull/7216

        var clientManagerGrain = GrainFactory.GetGrain<IBlockScanManagerGrain>(GrainIdHelper.GenerateBlockScanManagerGrainId());
        var ids = await clientManagerGrain.GetAllBlockScanIdsAsync();
        foreach (var (_, set) in ids)
        {
            foreach (var id in set)
            {
                var clientGrain = GrainFactory.GetGrain<IBlockScanGrain>(id);
                if (!await clientGrain.IsNeedRecoverAsync())
                {
                    continue;
                }

                var blockScanGrain = GrainFactory.GetGrain<IBlockScanExecutorGrain>(id);
                _ = Task.Run(blockScanGrain.HandleHistoricalBlockAsync);
            }
        }
    }

    public async Task Start()
    {
        if (_reminder != null)
        {
            return;
        }
        _reminder = await RegisterOrUpdateReminder(
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
        await UnregisterReminder(_reminder);
        _reminder = null;
    }
}