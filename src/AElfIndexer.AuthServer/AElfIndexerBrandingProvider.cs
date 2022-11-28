using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer;

[Dependency(ReplaceServices = true)]
public class AElfIndexerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "AElfIndexer";
}
