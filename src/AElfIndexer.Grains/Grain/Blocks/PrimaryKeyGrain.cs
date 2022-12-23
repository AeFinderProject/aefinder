using System.Threading.Tasks;
using AElfIndexer.Grains.State.Blocks;
using Microsoft.Extensions.Options;

namespace AElfIndexer.Grains.Grain.Blocks;

public class PrimaryKeyGrain: global::Orleans.Grain<PrimaryKeyState>, IPrimaryKeyGrain
{
    private readonly PrimaryKeyOptions _primaryKeyOptions;

    public PrimaryKeyGrain(IOptionsSnapshot<PrimaryKeyOptions> primaryKeyOptions)
    {
        _primaryKeyOptions = primaryKeyOptions.Value;
    }
    
    public override async Task OnActivateAsync()
    {
        await this.ReadStateAsync();
        if (this.State.SwitchInterval == 0)
        {
            this.State.SwitchInterval = _primaryKeyOptions.BlockGrainSwitchInterval;
        }
        await base.OnActivateAsync();
    }
    
    public override async Task OnDeactivateAsync()
    {
        await this.WriteStateAsync();
        await base.OnDeactivateAsync();
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