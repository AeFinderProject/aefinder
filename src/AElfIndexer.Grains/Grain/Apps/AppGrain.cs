using AElfIndexer.Grains.State.Apps;
using Orleans;

namespace AElfIndexer.Grains.Grain.Apps;

public class AppGrain : Grain<AppGrainState>, IAppGrain
{
    public async Task<ExistDto> Exists(string clientId)
    {
        if (string.IsNullOrWhiteSpace(State.ClientId))
        {
            State.ClientId = clientId;
            await WriteStateAsync();
            return new ExistDto() { Exists = true, Added = true };
        }

        return new ExistDto() { Exists = State.ClientId == clientId, Added = false };
    }
}