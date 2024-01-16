using AeFinder.Localization;
using Volo.Abp.Application.Services;

namespace AeFinder;

/* Inherit your application services from this class.
 */
public abstract class AeFinderAppService : ApplicationService
{
    protected AeFinderAppService()
    {
        LocalizationResource = typeof(AeFinderResource);
    }
}
