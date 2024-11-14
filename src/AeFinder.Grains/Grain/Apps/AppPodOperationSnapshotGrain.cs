using AeFinder.Apps.Dto;
using AeFinder.Apps.Eto;
using AeFinder.Grains.State.Apps;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Apps;

public class AppPodOperationSnapshotGrain : AeFinderGrain<AppPodOperationSnapshotState>, IAppPodOperationSnapshotGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    
    public AppPodOperationSnapshotGrain(IObjectMapper objectMapper,IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
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
        
        //Publish organization create eto to background worker
        var eto = _objectMapper.Map<AppPodOperationSnapshotState, AppPodOperationSnapshotCreateEto>(State);
        await _distributedEventBus.PublishAsync(eto);
    }
}