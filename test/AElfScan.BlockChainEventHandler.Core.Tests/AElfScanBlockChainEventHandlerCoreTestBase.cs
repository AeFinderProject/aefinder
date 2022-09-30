using AElfScan.AElf.DTOs;
using AElfScan.AElf.Processors;
using AElfScan.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.RabbitMq;

namespace AElfScan;

public abstract class AElfScanBlockChainEventHandlerCoreTestBase:AElfScanTestBase<AElfScanBlockChainEventHandlerCoreTestModule>
{

}