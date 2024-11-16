using AeFinder.Apps.Dto;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppPodOperationSnapshotGrain: IGrainWithStringKey
{
    Task<AppPodOperationSnapshotDto> GetAsync();

    Task SetAsync(AppPodOperationSnapshotDto dto);
}