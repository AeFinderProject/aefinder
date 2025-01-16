using AeFinder.Apps.Dto;
using AeFinder.Apps.Eto;
using AeFinder.Grains.State.Apps;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Apps;

public class AppPodSnapshotGrain : AeFinderGrain<List<AppPodOperationSnapshotState>>, IAppPodSnapshotGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    
    public AppPodSnapshotGrain(IObjectMapper objectMapper,IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public async Task<List<AppPodOperationSnapshotDto>> GetListAsync()
    {
        if (State == null)
        {
            return new List<AppPodOperationSnapshotDto>();
        }
        return _objectMapper.Map<List<AppPodOperationSnapshotState>, List<AppPodOperationSnapshotDto>>(State);
    }
    
    public async Task SetAsync(AppPodOperationSnapshotDto dto)
    {
        await ReadStateAsync();
        var snapshotState= _objectMapper.Map<AppPodOperationSnapshotDto, AppPodOperationSnapshotState>(dto);
        this.State.Add(snapshotState);
        await WriteStateAsync();
        
        //Publish organization create eto to background worker
        var eto = _objectMapper.Map<AppPodOperationSnapshotState, AppPodOperationSnapshotCreateEto>(snapshotState);
        await _distributedEventBus.PublishAsync(eto);
    }
}