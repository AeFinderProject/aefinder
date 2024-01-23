using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AElfIndexer.Dapp;

[DependsOn(typeof(AbpEventBusRabbitMqModule),
    typeof(AElfIndexerDappBaseModule))]
public class AElfIndexerDappModule : AbpModule
{
    
}