using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.Studio;

public interface IStudioService
{
    Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(string appId, AddOrUpdateAeFinderAppInput input);
    Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(string appId, ApplyAeFinderAppNameInput input);
    Task<AeFinderAppInfoDto> GetAeFinderApp(string clientId, GetAeFinderAppInfoInput input);
    Task<AddDeveloperToAppDto> AddDeveloperToApp(AddDeveloperToAppInput input);
    Task<List<AeFinderAppInfo>> GetAeFinderAppList();
    Task<string> SubmitSubscriptionInfoAsync(string clientId, SubscriptionInfo input);
    Task<QueryAeFinderAppDto> QueryAeFinderAppAsync(string clientId, QueryAeFinderAppInput input);
    Task<QueryAeFinderAppLogsDto> QueryAeFinderAppLogsAsync(string clientId, QueryAeFinderAppLogsInput input);
}