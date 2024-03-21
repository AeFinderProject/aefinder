using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AeFinder.Kubernetes;

public class AeFinderKubernetesModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<KubernetesOptions>(configuration.GetSection("Kubernetes"));
        context.Services.AddSingleton<k8s.Kubernetes>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KubernetesOptions>>().Value;
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(options.KubeConfigPath);
            return new k8s.Kubernetes(config);
        });
    }
}