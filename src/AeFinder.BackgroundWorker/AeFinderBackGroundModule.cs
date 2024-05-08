using AeFinder.App.Deploy;
using AeFinder.Kubernetes;
using AeFinder.Kubernetes.Manager;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AeFinder.BackgroundWorker;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AeFinderKubernetesModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderApplicationModule))]
public class AeFinderBackGroundModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AeFinderHostedService>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderBackGroundModule>(); });
        context.Services.AddTransient<IAppDeployManager, KubernetesAppManager>();
    }
}