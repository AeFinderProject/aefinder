using AeFinder.Apps.Dto;
using AeFinder.Grains.State.Apps;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Apps;

public class AppResourceLimitGrain : Grain<AppResourceLimitState>, IAppResourceLimitGrain
{
    private readonly IObjectMapper _objectMapper;
    
    public AppResourceLimitGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public Task<AppResourceLimitDto> GetAsync()
    {
        return Task.FromResult(_objectMapper.Map<AppResourceLimitState, AppResourceLimitDto>(State));
    }

    public async Task SetMaxEntityCallCountAsync(int maxEntityCallCount)
    {
        State.MaxEntityCallCount = maxEntityCallCount;
        await WriteStateAsync();
    }
    
    public async Task SetMaxEntitySizeAsync(int maxEntitySize)
    {
        State.MaxEntitySize = maxEntitySize;
        await WriteStateAsync();
    }
    
    public async Task SetMaxLogCallCountAsync(int maxLogCallCount)
    {
        State.MaxLogCallCount = maxLogCallCount;
        await WriteStateAsync();
    }
    
    public async Task SetMaxLogSizeAsync(int maxLogSize)
    {
        State.MaxLogSize = maxLogSize;
        await WriteStateAsync();
    }
    
    public async Task SetMaxContractCallCountAsync(int maxContractCallCount)
    {
        State.MaxContractCallCount = maxContractCallCount;
        await WriteStateAsync();
    }
    
    public async Task SetAppFullPodRequestCpuCoreAsync(string requestCpuCore)
    {
        State.AppFullPodRequestCpuCore = requestCpuCore;
        await WriteStateAsync();
    }
    
    public async Task SetAppFullPodRequestMemoryAsync(string requestMemory)
    {
        State.AppFullPodRequestMemory = requestMemory;
        await WriteStateAsync();
    }
    
    public async Task SetAppQueryPodRequestCpuCoreAsync(string requestCpuCore)
    {
        State.AppQueryPodRequestCpuCore = requestCpuCore;
        await WriteStateAsync();
    }
    
    public async Task SetAppQueryPodRequestMemoryAsync(string requestMemory)
    {
        State.AppQueryPodRequestMemory = requestMemory;
        await WriteStateAsync();
    }
}