using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppPodOperationSnapshotCreateHandler: IDistributedEventHandler<AppPodOperationSnapshotCreateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<AppPodOperationSnapshotIndex, string> _podOperationSnapshotEntityMappingRepository;
    private readonly IEntityMappingRepository<AppPodUsageDurationIndex, string> _podUsageDurationEntityMappingRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AppPodOperationSnapshotCreateHandler> _logger;

    public AppPodOperationSnapshotCreateHandler(IObjectMapper objectMapper,
        ILogger<AppPodOperationSnapshotCreateHandler> logger,
        IEntityMappingRepository<AppPodUsageDurationIndex, string> podUsageDurationEntityMappingRepository,
        IEntityMappingRepository<AppPodOperationSnapshotIndex, string> podOperationSnapshotEntityMappingRepository)
    {
        _podOperationSnapshotEntityMappingRepository = podOperationSnapshotEntityMappingRepository;
        _podUsageDurationEntityMappingRepository = podUsageDurationEntityMappingRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(AppPodOperationSnapshotCreateEto eventData)
    {
        var snapShotIndex =
            _objectMapper.Map<AppPodOperationSnapshotCreateEto, AppPodOperationSnapshotIndex>(eventData);
        await _podOperationSnapshotEntityMappingRepository.AddOrUpdateAsync(snapShotIndex);

        switch (eventData.PodOperationType)
        {
            case AppPodOperationType.Start:
            {
                var durationIndex =
                    _objectMapper.Map<AppPodOperationSnapshotCreateEto, AppPodUsageDurationIndex>(eventData);
                durationIndex.StartTimestamp = eventData.Timestamp;
                await _podUsageDurationEntityMappingRepository.AddOrUpdateAsync(durationIndex);
                break;
            }
            case AppPodOperationType.ResourceChange:
            {
                var queryable = await _podUsageDurationEntityMappingRepository.GetQueryableAsync();
                queryable = queryable.Where(p => p.AppId == eventData.AppId && p.AppVersion == eventData.AppVersion)
                    .OrderByDescending(p => p.StartTimestamp).Take(1);
                var list = queryable.ToList();
                if (list.Count == 0)
                {
                    _logger.LogWarning(
                        $"Unable to find {eventData.AppId} {eventData.AppVersion} latest duration record when {eventData.PodOperationType}.");
                }
                else
                {
                    var latestDurationIndex = list[0];
                    latestDurationIndex.EndTimestamp = eventData.Timestamp;
                    latestDurationIndex.TotalUsageDuration =
                        latestDurationIndex.EndTimestamp - latestDurationIndex.StartTimestamp;
                    await _podUsageDurationEntityMappingRepository.AddOrUpdateAsync(latestDurationIndex);
                }
                var durationIndex =
                    _objectMapper.Map<AppPodOperationSnapshotCreateEto, AppPodUsageDurationIndex>(eventData);
                durationIndex.StartTimestamp = eventData.Timestamp;
                await _podUsageDurationEntityMappingRepository.AddOrUpdateAsync(durationIndex);
                break;
            }
            case AppPodOperationType.Stop:
            {
                var queryable = await _podUsageDurationEntityMappingRepository.GetQueryableAsync();
                queryable = queryable.Where(p => p.AppId == eventData.AppId && p.AppVersion == eventData.AppVersion)
                    .OrderByDescending(p => p.StartTimestamp).Take(1);
                var list = queryable.ToList();
                if (list.Count == 0)
                {
                    _logger.LogWarning(
                        $"Unable to find {eventData.AppId} {eventData.AppVersion} latest duration record when {eventData.PodOperationType}.");
                }
                else
                {
                    var latestDurationIndex = list[0];
                    latestDurationIndex.EndTimestamp = eventData.Timestamp;
                    latestDurationIndex.TotalUsageDuration =
                        latestDurationIndex.EndTimestamp - latestDurationIndex.StartTimestamp;
                    await _podUsageDurationEntityMappingRepository.AddOrUpdateAsync(latestDurationIndex);
                }
                break;
            }
        }
        
    }
}