using System.Runtime.CompilerServices;
using AElfIndexer.Grains;
using AElfIndexer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfIndexer.EntityEventHandler.Core.Tests;

[DependsOn(typeof(AElfIndexerEntityEventHandlerCoreModule),
    typeof(AElfIndexerOrleansTestBaseModule),
    typeof(AElfIndexerDomainTestModule))]
public class AElfIndexerEntityEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AElfIndexerEntityEventHandlerCoreTestModule>();
        });
       // context.Services.Configure<AElfEntityMappingOptions>(options =>
       //                         {
       //                             options.CollectionPrefix = "AElfIndexerTestV1";
       //                             options.ShardInitSettings = InitShardInitSettingOptions();
       //                             options.CollectionTailSecondExpireTime = 30;
       //                         }); 
    }
      // private List<ShardInitSetting> InitShardInitSettingOptions()
      //              {
      //                  ShardInitSetting blockIndexDto = new ShardInitSetting();
      //                  blockIndexDto.CollectionName = "BlockIndex";
      //                  blockIndexDto.ShardGroups = new List<ShardGroup>()
      //                  {
      //                      new ShardGroup()
      //                      {
      //                          ShardKeys = new List<ShardKey>()
      //                          {
      //                              new ShardKey()
      //                              {
      //                                  Name = "ChainId",
      //                                  Value = "AELF",
      //                                  Step = "",
      //                                  StepType = StepType.None
      //                              },
      //                              new ShardKey()
      //                              {
      //                                  Name = "BlockHeight",
      //                                  Value = "0",
      //                                  Step = "5",
      //                                  StepType = StepType.Floor
      //                              }
      //                          }
      //                      },
      //                      new ShardGroup()
      //                      {
      //                          ShardKeys = new List<ShardKey>()
      //                          {
      //                              new ShardKey()
      //                              {
      //                                  Name = "ChainId",
      //                                  Value = "tDVV",
      //                                  Step = "",
      //                                  StepType = StepType.None
      //                              },
      //                              new ShardKey()
      //                              {
      //                                  Name = "BlockHeight",
      //                                  Value = "0",
      //                                  Step = "10",
      //                                  StepType = StepType.Floor
      //                              }
      //                          }
      //                      }
      //                  };
      //      
      //                  ShardInitSetting logEventIndexDto = new ShardInitSetting();
      //                  logEventIndexDto.CollectionName = "LogEventIndex";
      //                  logEventIndexDto.ShardGroups = new List<ShardGroup>()
      //                  {
      //                      new ShardGroup()
      //                      {
      //                          ShardKeys = new List<ShardKey>()
      //                          {
      //                              new ShardKey()
      //                              {
      //                                  Name = "ChainId",
      //                                  Value = "AELF",
      //                                  Step = "",
      //                                  StepType = StepType.None
      //                              },
      //                              new ShardKey()
      //                              {
      //                                  Name = "BlockHeight",
      //                                  Value = "0",
      //                                  Step = "2000",
      //                                  StepType = StepType.Floor
      //                              }
      //                          }
      //                      },
      //                      new ShardGroup()
      //                      {
      //                          ShardKeys = new List<ShardKey>()
      //                          {
      //                              new ShardKey()
      //                              {
      //                                  Name = "ChainId",
      //                                  Value = "tDVV",
      //                                  Step = "",
      //                                  StepType = StepType.None
      //                              },
      //                              new ShardKey()
      //                              {
      //                                  Name = "BlockHeight",
      //                                  Value = "0",
      //                                  Step = "1000",
      //                                  StepType = StepType.Floor
      //                              }
      //                          }
      //                      }
      //                  };
      //      
      //                  return new List<ShardInitSetting>()
      //                  {
      //                      blockIndexDto,
      //                      logEventIndexDto
      //                  };
      //      
      //              }
}