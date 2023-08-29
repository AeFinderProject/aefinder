using System.Collections.Generic;
using AElf.EntityMapping;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Options;
using AElf.EntityMapping.Sharding;
using AElfIndexer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.Extensions;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(
    typeof(AElfIndexerApplicationModule),
    typeof(AElfIndexerDomainTestModule),
    typeof(AElfIndexerOrleansTestBaseModule),
    typeof(AElfEntityMappingElasticsearchModule)
)]
public class AElfIndexerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {

        context.Services.Configure<ApiOptions>(o =>
        {
            o.BlockQueryHeightInterval = 1000;
            o.TransactionQueryHeightInterval = 1000;
            o.LogEventQueryHeightInterval = 1000;
        });
        // context.Services.Configure<AElfEntityMappingOptions>(options =>
        //         {
        //             options.CollectionPrefix = "AElfIndexerTestV1";
        //             options.ShardInitSettings = InitShardInitSettingOptions();
        //             options.CollectionTailSecondExpireTime = 30;
        //         });
    }

    // private List<ShardInitSetting> InitShardInitSettingOptions()
    //        {
    //            ShardInitSetting blockIndexDto = new ShardInitSetting();
    //            blockIndexDto.CollectionName = "BlockIndex";
    //            blockIndexDto.ShardGroups = new List<ShardGroup>()
    //            {
    //                new ShardGroup()
    //                {
    //                    ShardKeys = new List<ShardKey>()
    //                    {
    //                        new ShardKey()
    //                        {
    //                            Name = "ChainId",
    //                            Value = "AELF",
    //                            Step = "",
    //                            StepType = StepType.None
    //                        },
    //                        new ShardKey()
    //                        {
    //                            Name = "BlockHeight",
    //                            Value = "0",
    //                            Step = "5",
    //                            StepType = StepType.Floor
    //                        }
    //                    }
    //                },
    //                new ShardGroup()
    //                {
    //                    ShardKeys = new List<ShardKey>()
    //                    {
    //                        new ShardKey()
    //                        {
    //                            Name = "ChainId",
    //                            Value = "tDVV",
    //                            Step = "",
    //                            StepType = StepType.None
    //                        },
    //                        new ShardKey()
    //                        {
    //                            Name = "BlockHeight",
    //                            Value = "0",
    //                            Step = "10",
    //                            StepType = StepType.Floor
    //                        }
    //                    }
    //                }
    //            };
    //
    //            ShardInitSetting logEventIndexDto = new ShardInitSetting();
    //            logEventIndexDto.CollectionName = "LogEventIndex";
    //            logEventIndexDto.ShardGroups = new List<ShardGroup>()
    //            {
    //                new ShardGroup()
    //                {
    //                    ShardKeys = new List<ShardKey>()
    //                    {
    //                        new ShardKey()
    //                        {
    //                            Name = "ChainId",
    //                            Value = "AELF",
    //                            Step = "",
    //                            StepType = StepType.None
    //                        },
    //                        new ShardKey()
    //                        {
    //                            Name = "BlockHeight",
    //                            Value = "0",
    //                            Step = "2000",
    //                            StepType = StepType.Floor
    //                        }
    //                    }
    //                },
    //                new ShardGroup()
    //                {
    //                    ShardKeys = new List<ShardKey>()
    //                    {
    //                        new ShardKey()
    //                        {
    //                            Name = "ChainId",
    //                            Value = "tDVV",
    //                            Step = "",
    //                            StepType = StepType.None
    //                        },
    //                        new ShardKey()
    //                        {
    //                            Name = "BlockHeight",
    //                            Value = "0",
    //                            Step = "1000",
    //                            StepType = StepType.Floor
    //                        }
    //                    }
    //                }
    //            };
    //
    //            return new List<ShardInitSetting>()
    //            {
    //                blockIndexDto,
    //                logEventIndexDto
    //            };
    //
    //        }
}
