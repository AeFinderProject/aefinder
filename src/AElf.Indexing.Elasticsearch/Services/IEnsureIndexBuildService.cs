using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Indexing.Elasticsearch.Services
{
    public interface IEnsureIndexBuildService
    {
        void EnsureIndexesCreateAsync();
    }
    

    public class EnsureIndexBuildService : IEnsureIndexBuildService, ITransientDependency
    {
        private readonly IElasticIndexService _elasticIndexService;
        private readonly List<Type> _modules;
        private readonly IndexSettingOptions _indexSettingOptions;

        public EnsureIndexBuildService(IOptions<IndexCreateOption> moduleConfiguration,
            IElasticIndexService elasticIndexService, IOptions<IndexSettingOptions> indexSettingOptions)
        {
            _elasticIndexService = elasticIndexService;
            _modules = moduleConfiguration.Value.Modules;
            _indexSettingOptions = indexSettingOptions.Value;
        }

        public void EnsureIndexesCreateAsync()
        {
            AsyncHelper.RunSync(async () =>
            {
                foreach (var module in _modules)
                {
                    await HandleModuleAsync(module);
                }
            });
        }

        private async Task HandleModuleAsync(Type type)
        {
            var types = GetTypesAssignableFrom<IIndexBuild>(type.Assembly);
            foreach (var t in types)
            {
                await _elasticIndexService.CreateIndexAsync(t.Name.ToLower(), t, _indexSettingOptions.NumberOfShards,
                    _indexSettingOptions.NumberOfReplicas);
            }
        }

        private List<Type> GetTypesAssignableFrom<T>(Assembly assembly)
        {
            var compareType = typeof(T);
            return assembly.DefinedTypes
                .Where(type => compareType.IsAssignableFrom(type) && !compareType.IsAssignableFrom(type.BaseType) &&
                               !type.IsAbstract && type.IsClass && compareType != type)
                .Cast<Type>().ToList();
        }
    }
}