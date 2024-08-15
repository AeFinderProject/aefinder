namespace AeFinder.Grains.Grain.Apps;

public interface IAppDataClearManagerGrain: IGrainWithIntegerKey
{
    Task<Dictionary<string, string>> GetVersionClearTasksAsync();
    Task AddVersionClearTaskAsync(string appId, string version);
    Task RemoveVersionClearTaskAsync(string version);
}