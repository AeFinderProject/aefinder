using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch.Exceptions;
using AElf.Indexing.Elasticsearch.Provider;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;

namespace AElf.Indexing.Elasticsearch.Services
{
    public interface IElasticIndexService
    {
        Task CreateIndexAsync(string indexName, int shard = 1, int numberOfReplicas = 1);

        Task CreateIndexAsync<TEntity, TKey>(string indexName, int shard = 1, int numberOfReplicas = 1)
            where TEntity : class, IEntity<TKey>, new();
        
        Task CreateIndexAsync(string indexName, Type type, int shard = 1, int numberOfReplicas = 1);

        Task ReIndex<TEntity, TKey>(string indexName)
            where TEntity : class, IEntity<TKey>, new();

        Task DeleteIndexAsync(string indexName);

        Task ReBuild<TEntity, TKey>(string indexName)
            where TEntity : class, IEntity<TKey>, new();
    }

    public class ElasticIndexService : IElasticIndexService, ITransientDependency
    {
        private readonly IEsClientProvider _esClientProvider;
        private readonly ILogger<ElasticIndexService> _logger;

        public ElasticIndexService(IEsClientProvider esClientProvider, ILogger<ElasticIndexService> logger)
        {
            _esClientProvider = esClientProvider;
            _logger = logger;
        }

        private ElasticClient GetEsClient()
        {
            return _esClientProvider.GetClient();
        }

        /// <summary>
        /// CreateEsIndex Not Mapping
        /// Auto Set Alias alias is Input IndexName
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="shard"></param>
        /// <param name="numberOfReplicas"></param>
        /// <returns></returns>
        public async Task CreateIndexAsync(string indexName, int shard = 1, int numberOfReplicas = 1)
        {
            var client = GetEsClient();
            //var exits = await client.Indices.AliasExistsAsync(indexName);
            var exits = await client.Indices.ExistsAsync(indexName);

            if (exits.Exists)
                return;
            var newName = indexName;// + DateTime.Now.Ticks;
            var result = await client
                .Indices.CreateAsync(newName,
                    ss =>
                        ss.Index(newName)
                            .Settings(
                                o => o.NumberOfShards(shard).NumberOfReplicas(numberOfReplicas)
                                    .Setting("max_result_window", int.MaxValue)));
            if (!result.Acknowledged)
                throw new ElasticSearchException(
                    $"Crate Index {indexName} failed : :" + result.ServerError.Error.Reason);
            await client.Indices.PutAliasAsync(newName, indexName);
        }

        /// <summary>
        /// CreateEsIndex auto Mapping T Property
        /// Auto Set Alias alias is Input IndexName
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="indexName"></param>
        /// <param name="shard"></param>
        /// <param name="numberOfReplicas"></param>
        /// <returns></returns>
        public async Task CreateIndexAsync<TEntity, TKey>(string indexName, int shard = 1, int numberOfReplicas = 1)
            where TEntity : class, IEntity<TKey>, new()
        {
            var client = GetEsClient();
            var exits = await client.Indices.ExistsAsync(indexName);

            if (exits.Exists)
                return;
            var newName = indexName;// + DateTime.Now.Ticks;
            var result = await client
                .Indices.CreateAsync(newName,
                    ss =>
                        ss.Index(newName)
                            .Settings(
                                o => o.NumberOfShards(shard).NumberOfReplicas(numberOfReplicas)
                                    .Setting("max_result_window", int.MaxValue))
                            .Map(m => m.AutoMap<TEntity>()));
            if (!result.Acknowledged)
                throw new ElasticSearchException($"Create Index {indexName} failed : :" +
                                                 result.ServerError.Error.Reason);
            await client.Indices.PutAliasAsync(newName, indexName);
        }

        public async Task CreateIndexAsync(string indexName, Type type, int shard = 1, int numberOfReplicas = 1)
        {
            if (!type.IsClass || type.IsAbstract || !typeof(IIndexBuild).IsAssignableFrom(type))
            {
                _logger.LogInformation($" type: {type.FullName} invalid type");
                return;
            }
            var client = GetEsClient();
            var exits = await client.Indices.ExistsAsync(indexName);
            
            if (exits.Exists)
            {
                _logger.LogInformation($" index: {indexName} type: {type.FullName} existed");
                return;
            }
            _logger.LogInformation($"create index for type {type.FullName}  index name: {indexName}");
            //var newName = indexName + DateTime.Now.Ticks;
            var result = await client
                .Indices.CreateAsync(indexName,
                    ss =>
                        ss.Index(indexName)
                            .Settings(
                                o => o.NumberOfShards(shard).NumberOfReplicas(numberOfReplicas)
                                    .Setting("max_result_window", int.MaxValue))
                            .Map(m => m.AutoMap(type)));
            if (!result.Acknowledged)
                throw new ElasticSearchException($"Create Index {indexName} failed : :" +
                                                 result.ServerError.Error.Reason);
            //await client.Indices.PutAliasAsync(newName, indexName);
        }

        public async Task ReIndex<TEntity, TKey>(string indexName)
            where TEntity : class, IEntity<TKey>, new()
        {
            await DeleteIndexAsync(indexName);
            await CreateIndexAsync<TEntity, TKey>(indexName);
        }

        /// <summary>
        /// Delete Index
        /// </summary>
        /// <returns></returns>
        public async Task DeleteIndexAsync(string indexName)
        {
            var client = GetEsClient();
            var response = await client.Indices.DeleteAsync(indexName);
            if (response.Acknowledged) return;
            throw new Exception($"Delete index {indexName} failed :{response.ServerError.Error.Reason}");
        }

        /// <summary>
        /// Non-stop Update Documents
        /// </summary>
        /// <returns></returns>
        public async Task ReBuild<TEntity, TKey>(string indexName)
            where TEntity : class, IEntity<TKey>, new()
        {
            var client = GetEsClient();
            var result = await client.Indices.GetAliasAsync(indexName);
            var oldName = result.Indices.Keys.First();
            var newIndex = indexName + DateTime.Now.Ticks;
            var createResult = await client.Indices.CreateAsync(newIndex,
                c =>
                    c.Index(newIndex)
                        .Map(m => m.AutoMap<TEntity>()));
            if (!createResult.Acknowledged)
            {
                throw new Exception($"reBuild create newIndex {indexName} failed :{result.ServerError.Error.Reason}");
            }

            var reResult = await client.ReindexOnServerAsync(descriptor => descriptor
                .Source(source => source.Index(indexName))
                .Destination(dest => dest.Index(newIndex)));

            if (reResult.ServerError != null)
            {
                throw new Exception($"reBuild {indexName} datas failed :{reResult.ServerError.Error.Reason}");
            }

            var deleteResult = await client.Indices.DeleteAsync(oldName);
            var reAliasResult = await client.Indices.PutAliasAsync(newIndex, indexName);

            if (!deleteResult.Acknowledged)
            {
                throw new Exception(
                    $"reBuild delete old Index {oldName.Name}   failed :{deleteResult.ServerError.Error.Reason}");
            }

            if (!reAliasResult.IsValid)
            {
                throw new Exception($"reBuild set Alias {indexName}  failed :{reAliasResult.ServerError.Error.Reason}");
            }
        }
    }
}