using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockScanCheckGrain : global::Orleans.Grain, IBlockScanCheckGrain
{
    private IGrainReminder _reminder = null;
    private readonly BlockScanOptions _blockScanOptions;

    public BlockScanCheckGrain(IOptionsSnapshot<BlockScanOptions> blockScanOptions)
    {
        _blockScanOptions = blockScanOptions.Value;
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        // TODO: Use IManagementGrain to check the Grain status after the 4.0 release
        // https://github.com/dotnet/orleans/pull/7216

        var clientManagerGrain = GrainFactory.GetGrain<IBlockScanManagerGrain>(0);
        var allClientIds = await clientManagerGrain.GetAllBlockScanIdsAsync();
        foreach (var (_, clientIds) in allClientIds)
        {
            foreach (var clientId in clientIds)
            {
                var clientGrain = GrainFactory.GetGrain<IBlockScanInfoGrain>(clientId);
                var clientInfo = await clientGrain.GetClientInfoAsync();
                if (clientInfo.ScanModeInfo.ScanMode != ScanMode.HistoricalBlock ||
                    clientInfo.LastHandleHistoricalBlockTime >=
                    DateTime.UtcNow.AddMinutes(-_blockScanOptions.HistoricalPushRecoveryThreshold))
                {
                    continue;
                }

                var blockScanGrain = GrainFactory.GetGrain<IBlockScanGrain>(clientId);
                Task.Run(blockScanGrain.HandleHistoricalBlockAsync);
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