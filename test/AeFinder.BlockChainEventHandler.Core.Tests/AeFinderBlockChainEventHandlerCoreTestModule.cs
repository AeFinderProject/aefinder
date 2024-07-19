using AeFinder.BlockChainEventHandler.DTOs;
using AeFinder.BlockChainEventHandler.Processors;
using AeFinder.Grains.Grain.Blocks;
using AeFinder.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace AeFinder.BlockChainEventHandler;

[DependsOn(
    typeof(AeFinderBlockChainEventHandlerCoreModule),
    typeof(AbpEventBusModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AeFinderDomainModule))]
public class AeFinderBlockChainEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // context.Services.Configure<AElfEntityMappingOptions>(options =>
        //                 {
        //                     options.CollectionPrefix = "AeFinderTestV1";
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