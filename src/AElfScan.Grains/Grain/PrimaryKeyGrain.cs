using AElfScan.Grains.State;
using Microsoft.Extensions.Options;

namespace AElfScan.Grains.Grain;

public class PrimaryKeyGrain: global::Orleans.Grain<PrimaryKeyState>, IPrimaryKeyGrain
{
    private readonly PrimaryKeyOptions _primaryKeyOptions;

    public PrimaryKeyGrain(IOptionsSnapshot<PrimaryKeyOptions> primaryKeyOptions)
    {
        _primaryKeyOptions = primaryKeyOptions.Value;
    }
    
    public override Task OnActivateAsync()
    {
        this.State.SwitchInterval = _primaryKeyOptions.BlockGrainSwitchInterval;
        this.ReadStateAsync();
        return base.OnActivateAsync();
    }
    
    public override Task OnDeactivateAsync()
    {
        this.WriteStateAsync();
        return base.OnDeactivateAsync();
    }

    public async Task<string> GetCurrentGrainPrimaryKey(string chainId)
    {
        return chainId + "_" + this.State.GrainPrimaryKey;
    }
    
    public async Task<string> GetGrainPrimaryKey(string chainId, int blocksCount)
    {
        if (this.State.Counter > this.State.SwitchInterval)
        {
            this.State.GrainPrimaryKey = this.State.GrainPrimaryKey + 1;
            this.State.Counter = 0;
            return chainId + "_" + this.State.GrainPrimaryKey;
        }

        this.State.Counter = this.State.Counter + blocksCount;
        return chainId + "_" + this.State.GrainPrimaryKey;
    }
}