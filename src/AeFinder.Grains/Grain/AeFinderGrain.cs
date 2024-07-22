namespace AeFinder.Grains.Grain;

public class AeFinderGrain<TGrainState>: Grain<TGrainState>
{
    private bool _changingState = true;
    
    protected override async Task ReadStateAsync()
    {
        if (!_changingState)
        {
            return;
        }

        await base.ReadStateAsync();
        _changingState = false;
    }

    protected override async Task WriteStateAsync()
    {
        await BeginChangingStateAsync();
        await base.WriteStateAsync();
        _changingState = false;
    }

    protected Task BeginChangingStateAsync()
    {
        _changingState = true;
        return Task.CompletedTask;
    }
}