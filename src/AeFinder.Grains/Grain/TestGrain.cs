using AeFinder.Grains.State;
using Orleans;

namespace AeFinder.Grains.Grain;

public class TestGrain: Grain<TestState>, ITestGrain
{
    public async Task CountAsync()
    {
        State.Count = State.Count + 1;
        await WriteStateAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return State.Count;
    }
}