using System.Linq;
using AeFinder.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AeFinder.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AeFinderController : AbpControllerBase
{
    protected AeFinderController()
    {
        LocalizationResource = typeof(AeFinderResource);
    }

    protected string ClientId
    {
        get
        {
            return CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;

        }
    }
}
