using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AeFinder.Dapp;

[DependsOn(typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderDappBaseModule))]
public class AeFinderDappModule : AbpModule
{
    
}