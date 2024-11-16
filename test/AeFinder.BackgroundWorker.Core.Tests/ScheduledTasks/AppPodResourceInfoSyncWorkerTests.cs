using AeFinder.App.Es;
using AeFinder.BackgroundWorker.ScheduledTask;
using AElf.EntityMapping.Repositories;

namespace AeFinder.BackgroundWorker.Tests.ScheduledTasks;

public class AppPodResourceInfoSyncWorkerTests: AeFinderBackgroundWorkerCoreTestBase
{
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;
    private readonly AppPodResourceInfoSyncWorker _appPodResourceInfoSyncWorker;
    
    public AppPodResourceInfoSyncWorkerTests()
    {
        _appPodInfoEntityMappingRepository = GetRequiredService<IEntityMappingRepository<AppPodInfoIndex, string>>();
        _appPodResourceInfoSyncWorker = GetRequiredService<AppPodResourceInfoSyncWorker>();
    }
}