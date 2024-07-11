namespace AeFinder.Grains.Grain.BlockPush;

public interface IMessageStreamNamespaceGrain : IGrainWithStringKey
{
    Task AddAppAsync(string appId);
    Task<bool> ContainsAppAsync(string appId);
    Task<int> GetAppCountAsync();
}