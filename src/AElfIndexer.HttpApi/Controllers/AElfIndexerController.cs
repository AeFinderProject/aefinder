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
}
