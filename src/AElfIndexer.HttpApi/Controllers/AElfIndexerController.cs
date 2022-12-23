using System.Linq;
using AElfIndexer.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfIndexer.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AElfIndexerController : AbpControllerBase
{
    protected AElfIndexerController()
    {
        LocalizationResource = typeof(AElfIndexerResource);
    }

    protected string ClientId
    {
        get
        {
            return CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;

        }
    }
}
