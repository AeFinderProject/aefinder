using AeFinder.Apps.Dto;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppPodSnapshotGrain: IGrainWithStringKey
{
    Task<List<AppPodOperationSnapshotDto>> GetListAsync();

    Task SetAsync(AppPodOperationSnapshotDto dto);
}