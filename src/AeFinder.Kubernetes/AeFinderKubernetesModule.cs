using AeFinder.Kubernetes.Manager;
using AeFinder.Logger;
using AeFinder.Options;
using AElf.ExceptionHandler.ABP;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.Kubernetes;

[DependsOn(
    typeof(AOPExceptionModule)
)]
public class AeFinderKubernetesModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<KubernetesOptions>(configuration.GetSection("Kubernetes"));
        context.Services.AddSingleton<k8s.Kubernetes>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KubernetesOptions>>().Value;
            if (options == null)
            {
                throw new Exception("the config of [Kubernetes] is missing.");
            }
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(options.KubeConfigPath);
            return new k8s.Kubernetes(config);
        });
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
        KubernetesConstants.Initialize(configuration);
    }
}