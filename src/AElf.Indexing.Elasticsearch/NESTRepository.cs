using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch.Exceptions;
using AElf.Indexing.Elasticsearch.Options;
using AElf.Indexing.Elasticsearch.Provider;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.Domain.Entities;

namespace AElf.Indexing.Elasticsearch
{
    public class NESTRepository<TEntity, TKey> : NESTReaderRepository<TEntity, TKey>,
        INESTRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
        private readonly IndexSettingOptions _indexSettingOptions;

        public NESTRepository(IEsClientProvider esClientProvider,
            IOptionsSnapshot<IndexSettingOptions> indexSettingOptions, string index = null, string type = null)
            : base(esClientProvider, index, type)
        {
            _indexSettingOptions = indexSettingOptions.Value;
        }

        /// <summary>
        /// AddOrUpdate Document
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task AddOrUpdateAsync(TEntity model)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var exits = client.DocumentExists(DocumentPath<TEntity>.Id(new Id(model)), dd => dd.Index(indexName));

            if (exits.Exists)
            {
                var result = await client.UpdateAsync(DocumentPath<TEntity>.Id(new Id(model)),
                    ss => ss.Index(indexName).Doc(model).RetryOnConflict(3).Refresh(_indexSettingOptions.Refresh));

                if (result.ServerError == null) return;
                throw new ElasticSearchException($"Update Document failed at index{indexName} :" +
                                                 result.ServerError.Error.Reason);
            }
            else
            {
                var result = await client.IndexAsync(model, ss => ss.Index(indexName).Refresh(_indexSettingOptions.Refresh));
                if (result.ServerError == null) return;
                throw new ElasticSearchException($"Insert Docuemnt failed at index {indexName} :" +
                                                 result.ServerError.Error.Reason);
            }
        }
        
        /// <summary>
        /// Add Document
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task AddAsync(TEntity model)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var result = await client.IndexAsync(model, ss => ss.Index(indexName).Refresh(_indexSettingOptions.Refresh));
            if (result.ServerError == null) return;
            throw new ElasticSearchException($"Insert Docuemnt failed at index {indexName} :" +
                                             result.ServerError.Error.Reason);
        }

        /// <summary>
        /// Update Document
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task UpdateAsync(TEntity model)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var result = await client.UpdateAsync(DocumentPath<TEntity>.Id(new Id(model)),
                ss => ss.Index(indexName).Doc(model).RetryOnConflict(3).Refresh(_indexSettingOptions.Refresh));

            if (result.ServerError == null) return;
            throw new ElasticSearchException($"Update Document failed at index{indexName} :" +
                                             result.ServerError.Error.Reason);
        }
        
        /// <summary>
        /// Delete Document
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task DeleteAsync(TKey id)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var response = await client.DeleteAsync(new DeleteRequest(indexName, new Id(new {id = id.ToString()})) {Refresh = _indexSettingOptions.Refresh});
            if (response.ServerError == null) return;
            throw new Exception($"Delete Docuemnt at index {indexName} :{response.ServerError.Error.Reason}");
        }

        /// <summary>
        /// Delete Document
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task DeleteAsync(TEntity model)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var response = await client.DeleteAsync(new DeleteRequest(indexName, new Id(model)) {Refresh = _indexSettingOptions.Refresh});
            if (response.ServerError == null) return;
            throw new Exception($"Delete Docuemnt at index {indexName} :{response.ServerError.Error.Reason}");
        }

        /// <summary>
        /// Batch add and modify
        /// </summary>
        /// <param name="list"></param>
        /// <exception cref="ElasticSearchException"></exception>
        public async Task BulkAddOrUpdateAsync(List<TEntity> list)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var bulk = new BulkRequest(indexName)
            {
                Operations = new List<IBulkOperation>()
            };
            foreach (var item in list)
            {
                bulk.Operations.Add(new BulkIndexOperation<TEntity>(item));
            }
            var response = await client.BulkAsync(bulk);
            if (response.Errors)
            {
                throw new ElasticSearchException(
                    $"Bulk InsertOrUpdate Docuemnt failed at index {indexName} :{response.ServerError.Error.Reason}");
            }
        }

        /// <summary>
        /// Deleting Documents in Batches
        /// </summary>
        /// <param name="list"></param>
        /// <exception cref="ElasticSearchException"></exception>
        public async Task BulkDeleteAsync(List<TEntity> list)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var bulk = new BulkRequest(indexName)
            {
                Operations = new List<IBulkOperation>()
            };
            foreach (var item in list)
            {
                bulk.Operations.Add(new BulkDeleteOperation<TEntity>(new Id(item)));
            }

            var response = await client.BulkAsync(bulk);
            if (response.Errors)
            {
                throw new ElasticSearchException(
                    $"Bulk Delete Docuemnt at index {indexName} :{response.ServerError.Error.Reason}");
            }
        }
    }
}