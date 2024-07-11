namespace AeFinder.Grains.Grain.BlockPush;

public interface IMessageStreamNamespaceManagerGrain : IGrainWithIntegerKey
{
    Task<string> GetMessageStreamNamespaceAsync(string appId);
    Task<string> GetHistoricalMessageStreamNamespaceAsync(string appId);
}