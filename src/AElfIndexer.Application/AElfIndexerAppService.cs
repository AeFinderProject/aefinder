using System;
using System.Collections.Generic;
using System.Text;
using AElfIndexer.Localization;
using Volo.Abp.Application.Services;

namespace AElfIndexer;

/* Inherit your application services from this class.
 */
public abstract class AElfIndexerAppService : ApplicationService
{
    protected AElfIndexerAppService()
    {
        LocalizationResource = typeof(AElfIndexerResource);
    }
}
