using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;

namespace AeFinder.Studio;

public interface IStudioService
{
    Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(AddOrUpdateAeFinderAppInput input);
    Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput input);
    Task<AeFinderAppInfoDto> GetAeFinderApp();
    Task<AddDeveloperToAppDto> AddDeveloperToApp(AddDeveloperToAppInput input);
    Task<List<AeFinderAppInfo>> GetAeFinderAppList();
    Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input, SubscriptionManifestDto subscriptionManifest);
    Task<QueryAeFinderAppDto> QueryAeFinderAppAsync(QueryAeFinderAppInput input);
    Task<QueryAeFinderAppLogsDto> QueryAeFinderAppLogsAsync(QueryAeFinderAppLogsInput input);

    Task<string> GetAppIdAsync();

    Task<UpdateAeFinderAppDto> UpdateAeFinderAppAsync(UpdateAeFinderAppInput input);
}