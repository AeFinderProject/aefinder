namespace AeFinder.App.Deploy;

public interface IAppResourceLimitProvider
{
    Task<int> GetMaxEntityCallCountAsync(string appId);

    Task<int> GetMaxEntitySizeAsync(string appId);

    Task<int> GetMaxLogCallCountAsync(string appId);

    Task<int> GetMaxLogSizeAsync(string appId);

    Task<int> GetMaxContractCallCountAsync(string appId);

    Task<string> GetAppFullPodRequestCpuCoreAsync(string appId);

    Task<string> GetAppFullPodRequestMemoryAsync(string appId);

    Task<string> GetAppQueryPodRequestCpuCoreAsync(string appId);

    Task<string> GetAppQueryPodRequestMemoryAsync(string appId);
}