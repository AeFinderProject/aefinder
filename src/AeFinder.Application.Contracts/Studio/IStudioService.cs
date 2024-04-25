using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;

namespace AeFinder.Studio;

public interface IStudioService
{
    Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(AddOrUpdateAeFinderAppInput input);
    Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput input);
    Task<AeFinderAppInfoDto> GetAeFinderApp();
    Task<List<AeFinderAppInfo>> GetAeFinderAppList();
    Task DestroyAppAsync(string version);
    Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input, SubscriptionManifestDto subscriptionManifest);

    Task<string> GetAppIdAsync();

    Task<UpdateAeFinderAppDto> UpdateAeFinderAppAsync(UpdateAeFinderAppInput input);
    Task RestartAppAsync(string version);
}