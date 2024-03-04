using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AElfIndexer.App.Host;

[DependsOn(typeof(AbpEventBusRabbitMqModule),
    typeof(AElfIndexerAppHostBaseModule))]
public class AElfIndexerAppHostModule : AbpModule
{
    
}