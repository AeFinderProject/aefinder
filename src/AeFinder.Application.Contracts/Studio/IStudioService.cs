using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;

namespace AeFinder.Studio;

public interface IStudioService
{
    Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderAppAsync(AddOrUpdateAeFinderAppInput input);
    Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppNameAsync(ApplyAeFinderAppNameInput input);
    Task<AeFinderAppInfoDto> GetAeFinderAppAsync();
    Task<List<AeFinderAppInfo>> GetAeFinderAppListAsync();
    Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input, SubscriptionManifestDto subscriptionManifest);

    Task<string> GetAppIdAsync();

    Task<UpdateAeFinderAppDto> UpdateAeFinderAppAsync(UpdateAeFinderAppInput input);
}