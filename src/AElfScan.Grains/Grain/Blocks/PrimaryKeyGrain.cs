using AElfScan.Grains.State.Blocks;
using Microsoft.Extensions.Options;

namespace AElfScan.Grains.Grain.Blocks;

public class PrimaryKeyGrain: global::Orleans.Grain<PrimaryKeyState>, IPrimaryKeyGrain
{
    private readonly PrimaryKeyOptions _primaryKeyOptions;

    public PrimaryKeyGrain(IOptionsSnapshot<PrimaryKeyOptions> primaryKeyOptions)
    {
        _primaryKeyOptions = primaryKeyOptions.Value;
    }
    
    public override Task OnActivateAsync()
    {
        this.ReadStateAsync();
        if (this.State.SwitchInterval == 0)
        {
            this.State.SwitchInterval = _primaryKeyOptions.BlockGrainSwitchInterval;
        }
        return base.OnActivateAsync();
    }
    
    public override Task OnDeactivateAsync()
    {
        this.WriteStateAsync();
        return base.OnDeactivateAsync();
    }

    public async Task SetCounter(int blocksCount)
    {
        this.State.Counter = this.State.Counter + blocksCount;
    }

    public async Task<string> GetCurrentGrainPrimaryKey(string chainId)
    {
        if (string.IsNullOrEmpty(this.State.GrainPrimaryKey))
        {
            this.State.GrainPrimaryKey = await CreatePrimaryKey(chainId);
        }
        return this.State.GrainPrimaryKey;
    }
    
    public async Task<string> GetGrainPrimaryKey(string chainId)
    {
        if (this.State.Counter >= this.State.SwitchInterval)
        {
            this.State.SerialNumber = this.State.SerialNumber + 1;
            this.State.Counter = 0;
            await WriteStateAsync();
        }
        this.State.GrainPrimaryKey = await CreatePrimaryKey(chainId);
        return this.State.GrainPrimaryKey;
    }

    private async Task<string> CreatePrimaryKey(string chainId)
    {
        return chainId + "_" + this.State.SerialNumber; 
    }
}