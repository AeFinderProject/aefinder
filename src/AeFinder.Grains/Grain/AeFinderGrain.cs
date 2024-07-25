namespace AeFinder.Grains.Grain;

public class AeFinderGrain<TGrainState>: Grain<TGrainState>
{
    private bool _isStateConsistent = false;
    
    protected override async Task ReadStateAsync()
    {
        if (_isStateConsistent)
        {
            return;
        }

        await base.ReadStateAsync();
        _isStateConsistent = true;
    }

    protected override async Task WriteStateAsync()
    {
        await BeginChangingStateAsync();
        await base.WriteStateAsync();
        _isStateConsistent = true;
    }

    protected Task BeginChangingStateAsync()
    {
        _isStateConsistent = false;
        return Task.CompletedTask;
    }
}