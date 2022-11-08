using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace AElfScan;

[Dependency(ReplaceServices = true)]
public class AElfScanBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "AElfScan";
}
