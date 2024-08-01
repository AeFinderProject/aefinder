using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AeFinder.App;

[DependsOn(typeof(AeFinderAppHostBaseModule),
    typeof(AbpEventBusRabbitMqModule))]
public class AeFinderAppHostModule : AbpModule
{
    
}