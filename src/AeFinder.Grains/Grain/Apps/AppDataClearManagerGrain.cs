using AeFinder.Grains.State.Apps;

namespace AeFinder.Grains.Grain.Apps;

public class AppDataClearManagerGrain: Grain<AppDataClearManagerState>, IAppDataClearManagerGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetVersionClearTasksAsync()
    {
        return State.VersionClearTasksDictionary;
    }

    public async Task AddVersionClearTaskAsync(string appId, string version)
    {
        if (State.VersionClearTasksDictionary != null && State.VersionClearTasksDictionary.Keys.Contains(version))
        {
            return;
        }

        if (State.VersionClearTasksDictionary == null)
        {
            State.VersionClearTasksDictionary = new Dictionary<string, string>();
        }

        State.VersionClearTasksDictionary.Add(version, appId);

        await WriteStateAsync();
    }
    
    public async Task RemoveVersionClearTaskAsync(string version)
    {
        if (State.VersionClearTasksDictionary == null)
        {
            return;
        }

        if (State.VersionClearTasksDictionary.Keys.Contains(version))
        {
            State.VersionClearTasksDictionary.Remove(version);
            await WriteStateAsync();
        }
    }
}