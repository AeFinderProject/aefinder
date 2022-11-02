using Orleans;
using Orleans.Runtime;

namespace AElfScan.Grains.Grain.BlockScan;

public class BlockScanCheckGrain : global::Orleans.Grain, IBlockScanCheckGrain
{
    private IGrainReminder _reminder = null;
    
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        var clientManagerGrain = GrainFactory.GetGrain<IClientManagerGrain>(0);
        var allClientIds = await clientManagerGrain.GetAllClientIdsAsync();
        foreach (var (_, clientIds) in allClientIds)
        {
            foreach (var clientId in clientIds)
            {
                var clientGrain = GrainFactory.GetGrain<IClientGrain>(clientId);
                var clientInfo = await clientGrain.GetClientInfoAsync();
                if (clientInfo.ScanModeInfo.ScanMode != ScanMode.HistoricalBlock ||
                    clientInfo.LastHandleHistoricalBlockTime >= DateTime.UtcNow.AddMinutes(-5))
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