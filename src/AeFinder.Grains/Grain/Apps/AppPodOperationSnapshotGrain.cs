using AeFinder.Apps.Dto;
using AeFinder.Grains.State.Apps;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Apps;

public class AppPodOperationSnapshotGrain : AeFinderGrain<AppPodOperationSnapshotState>, IAppPodOperationSnapshotGrain
{
    private readonly IObjectMapper _objectMapper;
    
    public AppPodOperationSnapshotGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public Task<AppPodOperationSnapshotDto> GetAsync()
    {
        return Task.FromResult(_objectMapper.Map<AppPodOperationSnapshotState, AppPodOperationSnapshotDto>(State));
    }
    
    public async Task SetAsync(AppPodOperationSnapshotDto dto)
    {
        this.State = _objectMapper.Map<AppPodOperationSnapshotDto, AppPodOperationSnapshotState>(dto);
        await WriteStateAsync();
    }
}