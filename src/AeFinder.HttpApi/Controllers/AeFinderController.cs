using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Localization;
using Orleans;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Users;

namespace AeFinder.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AeFinderController : AbpControllerBase
{
    private readonly IClusterClient _clusterClient;

    protected AeFinderController(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        LocalizationResource = typeof(AeFinderResource);
    }

    protected string ClientId
    {
        get { return CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value; }
    }

    protected async Task<string> GetAppId()
    {
        if (CurrentUser == null)
        {
            throw new UserFriendlyException("User not found");
        }

        var userId = CurrentUser.GetId().ToString("N");
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var appInfo = await appGrain.GetAppInfo();
        return appInfo.AppId;
    }
}