using System;
using System.Collections.Generic;
using System.Text;
using AElfScan.Localization;
using Volo.Abp.Application.Services;

namespace AElfScan;

/* Inherit your application services from this class.
 */
public abstract class AElfScanAppService : ApplicationService
{
    protected AElfScanAppService()
    {
        LocalizationResource = typeof(AElfScanResource);
    }
}
