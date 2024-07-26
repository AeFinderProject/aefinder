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

    public async Task SetAsync(SetAppResourceLimitDto dto)
    {
        if (dto.MaxEntityCallCount > 0)
        {
            State.MaxEntityCallCount = dto.MaxEntityCallCount;
        }

        if (dto.MaxEntitySize > 0)
        {
            State.MaxEntitySize = dto.MaxEntitySize;
        }

        if (dto.MaxLogCallCount > 0)
        {
            State.MaxLogCallCount = dto.MaxLogCallCount;
        }

        if (dto.MaxLogSize > 0)
        {
            State.MaxLogSize = dto.MaxLogSize;
        }

        if (dto.MaxContractCallCount > 0)
        {
            State.MaxContractCallCount = dto.MaxContractCallCount;
        }

        if (!dto.AppFullPodRequestCpuCore.IsNullOrEmpty())
        {
            State.AppFullPodRequestCpuCore = dto.AppFullPodRequestCpuCore;
        }

        if (!dto.AppFullPodRequestMemory.IsNullOrEmpty())
        {
            State.AppFullPodRequestMemory = dto.AppFullPodRequestMemory;
        }

        if (!dto.AppQueryPodRequestCpuCore.IsNullOrEmpty())
        {
            State.AppQueryPodRequestCpuCore = dto.AppQueryPodRequestCpuCore;
        }

        if (!dto.AppQueryPodRequestMemory.IsNullOrEmpty())
        {
            State.AppQueryPodRequestMemory = dto.AppQueryPodRequestMemory;
        }
        
        await WriteStateAsync();
    }
}