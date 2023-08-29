using AElfIndexer.DTOs;
using AElfIndexer.Processors;
using AElfIndexer.Grains.Grain.Blocks;
using AElfIndexer.Orleans.TestBase;
using AElfIndexer.Providers;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(
    typeof(AElfIndexerBlockChainEventHandlerCoreModule),
    typeof(AbpEventBusModule),
    typeof(AElfIndexerOrleansTestBaseModule),
    typeof(AElfIndexerDomainModule))]
public class AElfIndexerBlockChainEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<PrimaryKeyOptions>(o =>
        {
            o.BlockGrainSwitchInterval = 100;
        });
        // context.Services.Configure<AElfEntityMappingOptions>(options =>
        //                 {
        //                     options.CollectionPrefix = "AElfIndexerTestV1";
        //                     options.ShardInitSettings = InitShardInitSettingOptions();
        //                     options.CollectionTailSecondExpireTime = 30;
        //                 });
        context.Services.AddTransient<IDistributedEventHandler<BlockChainDataEto>>(sp =>
            sp.GetService<BlockChainDataEventHandler>());
            
    }
     // private List<ShardInitSetting> InitShardInitSettingOptions()
     //           {
     //               ShardInitSetting blockIndexDto = new ShardInitSetting();
     //               blockIndexDto.CollectionName = "BlockIndex";
     //               blockIndexDto.ShardGroups = new List<ShardGroup>()
     //               {
     //                   new ShardGroup()
     //                   {
     //                       ShardKeys = new List<ShardKey>()
     //                       {
     //                           new ShardKey()
     //                           {
     //                               Name = "ChainId",
     //                               Value = "AELF",
     //                               Step = "",
     //                               StepType = StepType.None
     //                           },
     //                           new ShardKey()
     //                           {
     //                               Name = "BlockHeight",
     //                               Value = "0",
     //                               Step = "5",
     //                               StepType = StepType.Floor
     //                           }
     //                       }
     //                   },
     //                   new ShardGroup()
     //                   {
     //                       ShardKeys = new List<ShardKey>()
     //                       {
     //                           new ShardKey()
     //                           {
     //                               Name = "ChainId",
     //                               Value = "tDVV",
     //                               Step = "",
     //                               StepType = StepType.None
     //                           },
     //                           new ShardKey()
     //                           {
     //                               Name = "BlockHeight",
     //                               Value = "0",
     //                               Step = "10",
     //                               StepType = StepType.Floor
     //                           }
     //                       }
     //                   }
     //               };
     //   
     //               ShardInitSetting logEventIndexDto = new ShardInitSetting();
     //               logEventIndexDto.CollectionName = "LogEventIndex";
     //               logEventIndexDto.ShardGroups = new List<ShardGroup>()
     //               {
     //                   new ShardGroup()
     //                   {
     //                       ShardKeys = new List<ShardKey>()
     //                       {
     //                           new ShardKey()
     //                           {
     //                               Name = "ChainId",
     //                               Value = "AELF",
     //                               Step = "",
     //                               StepType = StepType.None
     //                           },
     //                           new ShardKey()
     //                           {
     //                               Name = "BlockHeight",
     //                               Value = "0",
     //                               Step = "2000",
     //                               StepType = StepType.Floor
     //                           }
     //                       }
     //                   },
     //                   new ShardGroup()
     //                   {
     //                       ShardKeys = new List<ShardKey>()
     //                       {
     //                           new ShardKey()
     //                           {
     //                               Name = "ChainId",
     //                               Value = "tDVV",
     //                               Step = "",
     //                               StepType = StepType.None
     //                           },
     //                           new ShardKey()
     //                           {
     //                               Name = "BlockHeight",
     //                               Value = "0",
     //                               Step = "1000",
     //                               StepType = StepType.Floor
     //                           }
     //                       }
     //                   }
     //               };
     //   
     //               return new List<ShardInitSetting>()
     //               {
     //                   blockIndexDto,
     //                   logEventIndexDto
     //               };
     //   
     //           }
}