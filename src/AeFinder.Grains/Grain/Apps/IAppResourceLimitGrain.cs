using AeFinder.Apps.Dto;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppResourceLimitGrain: IGrainWithStringKey
{
    Task<AppResourceLimitDto> GetAsync();

    Task SetMaxEntityCallCountAsync(int maxEntityCallCount);

    Task SetMaxEntitySizeAsync(int maxEntitySize);

    Task SetMaxLogCallCountAsync(int maxLogCallCount);

    Task SetMaxLogSizeAsync(int maxLogSize);

    Task SetMaxContractCallCountAsync(int maxContractCallCount);

    Task SetAppFullPodRequestCpuCoreAsync(string requestCpuCore);

    Task SetAppFullPodRequestMemoryAsync(string requestMemory);

    Task SetAppQueryPodRequestCpuCoreAsync(string requestCpuCore);

    Task SetAppQueryPodRequestMemoryAsync(string requestMemory);
}