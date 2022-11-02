using System.Collections.Generic;
using System.Linq;
using AElf.Indexing.Elasticsearch.Options;
using AElf.Indexing.Elasticsearch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.Indexing.Elasticsearch
{

    [DependsOn(
        typeof(AbpAutofacModule)
    )]
    public class AElfIndexingElasticsearchModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var serviceProvider = context.Services;
            serviceProvider.AddTransient(typeof(INESTReaderRepository<,>), typeof(NESTReaderRepository<,>));
            serviceProvider.AddTransient(typeof(INESTRepository<,>), typeof(NESTRepository<,>));
            serviceProvider.AddTransient(typeof(INESTWriterRepository<,>), typeof(NESTRepository<,>));
            var configuration = context.Services.GetConfiguration();
            Configure<EsEndpointOption>(options =>
            {
                if (configuration.GetChildren().Any(item => item.Key == "ElasticUris"))
                {
                    configuration.GetSection("ElasticUris").Bind(options);
                }
                else
                {
                    options.Uris = new List<string> { "http://127.0.0.1:9200"};
                }
            });
            Configure<IndexSettingOptions>(configuration.GetSection("IndexSetting"));
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var ensureIndexBuildService = context.ServiceProvider.GetService<IEnsureIndexBuildService>();
            ensureIndexBuildService?.EnsureIndexesCreateAsync();
        }
    }
}
