using AElfScan.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScan.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AElfScanController : AbpControllerBase
{
    protected AElfScanController()
    {
        LocalizationResource = typeof(AElfScanResource);
    }
}
