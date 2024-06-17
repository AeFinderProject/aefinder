namespace AeFinder.App.BlockState;

public interface IAppBlockStateChangeProvider
{
    Task SetChangeKeysAsync(string chainId, Dictionary<long, HashSet<string>> changeKeys);
    Task<HashSet<string>> GetChangeKeysAsync(string chainId, long blockHeight);
    Task CleanAsync(string chainId, long libHeight);
}