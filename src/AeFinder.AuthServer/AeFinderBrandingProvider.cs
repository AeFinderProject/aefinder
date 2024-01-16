using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AeFinder;

[Dependency(ReplaceServices = true)]
public class AeFinderBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "AeFinder";
}
