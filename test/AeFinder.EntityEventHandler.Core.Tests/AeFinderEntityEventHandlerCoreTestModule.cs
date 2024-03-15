using AeFinder.Orleans.TestBase;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AeFinder.EntityEventHandler;

[DependsOn(typeof(AeFinderEntityEventHandlerCoreModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AeFinderDomainTestModule))]
public class AeFinderEntityEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AeFinderEntityEventHandlerCoreTestModule>();
        });
       // context.Services.Configure<AElfEntityMappingOptions>(options =>
       //                         {
       //                             options.CollectionPrefix = "AeFinderTestV1";
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