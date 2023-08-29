using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Elasticsearch.Options;
using AElf.EntityMapping.Options;
using AElf.EntityMapping.Sharding;
using AElfIndexer.MongoDB;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElfIndexer;

[DependsOn(
    typeof(AElfIndexerMongoDbTestModule)
    )]
public class AElfIndexerDomainTestModule : AbpModule
{

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<CollectionCreateOptions>(x =>
        {
            x.AddModule(typeof(AElfIndexerDomainModule));
        });
        
        // // Do not modify this!!!
        // context.Services.Configure<EsEndpointOption>(options =>
        // {
        //     options.Uris = new List<string> { "http://127.0.0.1:9200"};
        // });
            
        /*context.Services.Configure<IndexSettingOptions>(options =>
        {
            options.NumberOfReplicas = 1;
            options.NumberOfShards = 1;
            options.Refresh = Refresh.True;
            options.IndexPrefix = "AElfIndexer";
        });*/
        context.Services.Configure<AElfEntityMappingOptions>(options =>
        {
            options.CollectionPrefix = "AElfIndexerTestV1";
            // options.ShardInitSettings = InitShardInitSettingOptions();
        });
        context.Services.Configure<ElasticsearchOptions>(
            options =>
            {
                options.NumberOfShards = 1;
                options.NumberOfReplicas = 1;
                options.Refresh = Refresh.True;
            }
        );
    }
    
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        // var elasticIndexService = context.ServiceProvider.GetRequiredService<IElasticIndexService>();
        /*var modules = context.ServiceProvider.GetRequiredService<IOptions<IndexCreateOption>>().Value.Modules;
            
        modules.ForEach(m =>
        {
            var types = GetTypesAssignableFrom<IIndexBuild>(m.Assembly);
            foreach (var t in types)
            {
                AsyncHelper.RunSync(async () =>
                    await elasticIndexService.DeleteIndexAsync("aelfindexer." + t.Name.ToLower()));
            }
        });*/
        
        var option = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AElfEntityMappingOptions>>();
        if(option.Value.CollectionPrefix.IsNullOrEmpty())
            return;
        
        var clientProvider = context.ServiceProvider.GetRequiredService<IElasticsearchClientProvider>();
        var client = clientProvider.GetClient();
        var indexPrefix = option.Value.CollectionPrefix.ToLower();
        
        client.Indices.Delete(indexPrefix+"*");
        client.Indices.DeleteTemplate(indexPrefix + "*");
    }
    
    private List<Type> GetTypesAssignableFrom<T>(Assembly assembly)
    {
        var compareType = typeof(T);
        return assembly.DefinedTypes
            .Where(type => compareType.IsAssignableFrom(type) && !compareType.IsAssignableFrom(type.BaseType) &&
                           !type.IsAbstract && type.IsClass && compareType != type)
            .Cast<Type>().ToList();
    }

    // private List<ShardInitSettingDto> InitShardInitSettingOptions()
    // {
    //     ShardInitSettingDto blockIndexDto = new ShardInitSettingDto();
    //     blockIndexDto.IndexName = "BlockIndex";
    //     blockIndexDto.ShardGroups = new List<ShardGroup>()
    //     {
    //         new ShardGroup()
    //         {
    //             ShardKeys = new List<ShardKey>()
    //             {
    //                 new ShardKey()
    //                 {
    //                     Name = "ChainId",
    //                     Value = "AELF",
    //                     Step = "",
    //                     StepType = 0,
    //                     GroupNo = "0"
    //                 },
    //                 new ShardKey()
    //                 {
    //                     Name = "BlockHeight",
    //                     Value = "0",
    //                     Step = "2000",
    //                     StepType = 1,
    //                     GroupNo = "0"
    //                 }
    //             }
    //         },
    //         new ShardGroup()
    //         {
    //             ShardKeys = new List<ShardKey>()
    //             {
    //                 new ShardKey()
    //                 {
    //                     Name = "ChainId",
    //                     Value = "tDVV",
    //                     Step = "",
    //                     StepType = 0,
    //                     GroupNo = "1"
    //                 },
    //                 new ShardKey()
    //                 {
    //                     Name = "BlockHeight",
    //                     Value = "0",
    //                     Step = "1000",
    //                     StepType = 1,
    //                     GroupNo = "1"
    //                 }
    //             }
    //         }
    //     };
    //
    //     ShardInitSettingDto transactionIndexDto = new ShardInitSettingDto();
    //     transactionIndexDto.IndexName = "TransactionIndex";
    //     transactionIndexDto.ShardGroups = new List<ShardGroup>()
    //     {
    //         new ShardGroup()
    //         {
    //             ShardKeys = new List<ShardKey>()
    //             {
    //                 new ShardKey()
    //                 {
    //                     Name = "ChainId",
    //                     Value = "AELF",
    //                     Step = "",
    //                     StepType = 0,
    //                     GroupNo = "0"
    //                 },
    //                 new ShardKey()
    //                 {
    //                     Name = "BlockHeight",
    //                     Value = "0",
    //                     Step = "2000",
    //                     StepType = 1,
    //                     GroupNo = "0"
    //                 }
    //             }
    //         },
    //         new ShardGroup()
    //         {
    //             ShardKeys = new List<ShardKey>()
    //             {
    //                 new ShardKey()
    //                 {
    //                     Name = "ChainId",
    //                     Value = "tDVV",
    //                     Step = "",
    //                     StepType = 0,
    //                     GroupNo = "1"
    //                 },
    //                 new ShardKey()
    //                 {
    //                     Name = "BlockHeight",
    //                     Value = "0",
    //                     Step = "1000",
    //                     StepType = 1,
    //                     GroupNo = "1"
    //                 }
    //             }
    //         }
    //     };
    //
    //     ShardInitSettingDto logEventIndexDto = new ShardInitSettingDto();
    //     logEventIndexDto.IndexName = "LogEventIndex";
    //     logEventIndexDto.ShardGroups = new List<ShardGroup>()
    //     {
    //         new ShardGroup()
    //         {
    //             ShardKeys = new List<ShardKey>()
    //             {
    //                 new ShardKey()
    //                 {
    //                     Name = "ChainId",
    //                     Value = "AELF",
    //                     Step = "",
    //                     StepType = 0,
    //                     GroupNo = "0"
    //                 },
    //                 new ShardKey()
    //                 {
    //                     Name = "BlockHeight",
    //                     Value = "0",
    //                     Step = "2000",
    //                     StepType = 1,
    //                     GroupNo = "0"
    //                 }
    //             }
    //         },
    //         new ShardGroup()
    //         {
    //             ShardKeys = new List<ShardKey>()
    //             {
    //                 new ShardKey()
    //                 {
    //                     Name = "ChainId",
    //                     Value = "tDVV",
    //                     Step = "",
    //                     StepType = 0,
    //                     GroupNo = "1"
    //                 },
    //                 new ShardKey()
    //                 {
    //                     Name = "BlockHeight",
    //                     Value = "0",
    //                     Step = "1000",
    //                     StepType = 1,
    //                     GroupNo = "1"
    //                 }
    //             }
    //         }
    //     };
    //
    //     return new List<ShardInitSettingDto>()
    //     {
    //         blockIndexDto,
    //         transactionIndexDto,
    //         logEventIndexDto
    //     };
    //
    // }
}
