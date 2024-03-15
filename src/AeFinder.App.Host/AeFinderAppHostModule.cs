using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AeFinder.App;

[DependsOn(typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderAppHostBaseModule))]
public class AeFinderAppHostModule : AbpModule
{
    
}