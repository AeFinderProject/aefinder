using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch.Provider;
using AElf.Indexing.Elasticsearch.Translator;
using Nest;
using Volo.Abp.Domain.Entities;

namespace AElf.Indexing.Elasticsearch
{
    public class NESTReaderRepository<TEntity, TKey> : NESTBaseRepository<TEntity, TKey>, 
        INESTReaderRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
        public NESTReaderRepository(IEsClientProvider esClientProvider, string index = null, string type = null)
            : base(esClientProvider, index, type)
        {

        }
        
        public async Task<TEntity> GetAsync(TKey id)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var selector = new Func<GetDescriptor<TEntity>, IGetRequest>(s => s
                .Index(indexName));
            var result =
                await client.GetAsync(new Nest.DocumentPath<TEntity>(new Id(new {id = id.ToString()})),
                    selector);
            return result.Found ? result.Source : null;
        }
        
        public async Task<TEntity> GetAsync(Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
            Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
            Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending)
        {
            Func<SearchDescriptor<TEntity>, ISearchRequest> selector = null;
            var indexName = IndexName;
            var client = GetElasticClient();
            if (sortExp != null)
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Sort(st => st.Field(sortExp, sortType))
                    .Source(includeFieldFunc ?? (i => i.IncludeAll())));
            }
            else
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Source(includeFieldFunc ?? (i => i.IncludeAll())));
            }

            var result = await client.SearchAsync(selector);
            return result.Total > 0 ? result.Documents.FirstOrDefault() : null;
        }
        
        public async Task<TEntity> GetByExpAsync(Expression<Func<TEntity, bool>> filterExp = null,
            Expression<Func<TEntity, object>> includeFieldExp = null,
            Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending)
        {
            Func<QueryContainerDescriptor<TEntity>, QueryContainer> filter = null;
            if (filterExp != null)
            {
                filter = q => Builders<TEntity>.Filter.Where(filterExp).Query;
            }
            else
            {
                filter = q => q.MatchAll();
            }

            Func<SourceFilterDescriptor<TEntity>, ISourceFilter> project = null;
            project = includeFieldExp != null ? IncludeFields(includeFieldExp) : i => i.IncludeAll();

            Func<SearchDescriptor<TEntity>, ISearchRequest> selector = null;
            var client = GetElasticClient();
            var indexName = IndexName;
            if (sortExp != null)
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filter)
                    .Sort(st => st.Field(sortExp, sortType))
                    .Source(project));
            }
            else
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filter)
                    .Source(project));
            }

            var result = await client.SearchAsync(selector);
            return result.Total > 0 ? result.Documents.FirstOrDefault() : null;
        }
        
        public async Task<Tuple<long, List<TEntity>>> GetListByExpAsync(Expression<Func<TEntity, bool>> filterExp = null,
            Expression<Func<TEntity, object>> includeFieldExp = null,
            Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending
           , int limit = 1000, int skip = 0)
        {
            Func<QueryContainerDescriptor<TEntity>, QueryContainer> filter = null;
            if (filterExp != null)
            {
                filter = q => Builders<TEntity>.Filter.Where(filterExp).Query;
            }
            else
            {
                filter = q => q.MatchAll();
            }

            Func<SourceFilterDescriptor<TEntity>, ISourceFilter> project = null;
            project = includeFieldExp != null ? IncludeFields(includeFieldExp) : i => i.IncludeAll();

            Func<SearchDescriptor<TEntity>, ISearchRequest> selector = null;
            var indexName = IndexName;
            var client = GetElasticClient();
            if (sortExp != null)
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filter)
                    .Sort(st => st.Field(sortExp, sortType))
                    .Source(project)
                    .From(skip)
                    .Size(limit));
            }
            else
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filter)
                    .Source(project)
                    .From(skip)
                    .Size(limit));
            }

            var result = await client.SearchAsync(selector);
            return new Tuple<long, List<TEntity>>(result.Total, result.Documents.ToList());
        }
        
        public async Task<Tuple<long, List<TEntity>>> GetListAsync(Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
            Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
            Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending
           , int limit = 1000, int skip = 0)
        {
            Func<SearchDescriptor<TEntity>, ISearchRequest> selector = null;
            var indexName = IndexName;
            var client = GetElasticClient();
            if (sortExp != null)
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Sort(st => st.Field(sortExp, sortType))
                    .Source(includeFieldFunc ?? (i => i.IncludeAll()))
                    .From(skip)
                    .Size(limit));
            }
            else
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Source(includeFieldFunc ?? (i => i.IncludeAll()))
                    .From(skip)
                    .Size(limit));
            }

            var result = await client.SearchAsync(selector);
            return new Tuple<long, List<TEntity>>(result.Total, result.Documents.ToList());
        }
        
        public async Task<Tuple<long, List<TEntity>>> GetSortListAsync(Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
            Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
            Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null
            , int limit = 1000, int skip = 0)
        {
            Func<SearchDescriptor<TEntity>, ISearchRequest> selector = null;
            var indexName = IndexName;
            var client = GetElasticClient();
            if (sortFunc != null)
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Sort(sortFunc)
                    .Source(includeFieldFunc ?? (i => i.IncludeAll()))
                    .From(skip)
                    .Size(limit));
            }
            else
            {
                selector = new Func<SearchDescriptor<TEntity>, ISearchRequest>(s => s
                    .Index(indexName)
                    .Query(filterFunc ?? (q => q.MatchAll()))
                    .Source(includeFieldFunc ?? (i => i.IncludeAll()))
                    .From(skip)
                    .Size(limit));
            }

            var result = await client.SearchAsync(selector);
            return new Tuple<long, List<TEntity>>(result.Total, result.Documents.ToList());
        }
        
        /// <summary>
        /// search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="skip">skip num</param>
        /// <param name="size">return document size</param>
        /// <param name="includeFields">return fields</param>
        /// <param name="preTags">Highlight tags</param>
        /// <param name="postTags">Highlight tags</param>
        /// <param name="disableHigh"></param>
        /// <param name="highField">Highlight fields</param>
        /// <returns></returns>
        public async Task<ISearchResponse<TEntity>> SearchAsync(SearchDescriptor<TEntity> query,
            int skip, int size, string[] includeFields = null,
            string preTags = "<strong style=\"color: red;\">", string postTags = "</strong>", bool disableHigh = false,
            params string[] highField)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            query.Index(indexName);
            var highlight = new HighlightDescriptor<TEntity>();
            if (disableHigh)
            {
                preTags = "";
                postTags = "";
            }

            highlight.PreTags(preTags).PostTags(postTags);

            var isHigh = highField != null && highField.Length > 0;

            var hfs = new List<Func<HighlightFieldDescriptor<TEntity>, IHighlightField>>();

            //分页
            query.Skip(skip).Take(size);
            //关键词高亮
            if (isHigh)
            {
                foreach (var s in highField)
                {
                    hfs.Add(f => f.Field(s));
                }
            }

            highlight.Fields(hfs.ToArray());
            query.Highlight(h => highlight);
            if (includeFields != null)
                query.Source(ss => ss.Includes(ff => ff.Fields(includeFields.ToArray())));
            var response = await client.SearchAsync<TEntity>(query);
            return response;
        }

        public virtual async Task<CountResponse> CountAsync(
            Func<QueryContainerDescriptor<TEntity>, QueryContainer> query)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            var response = await client.CountAsync<TEntity>(c => c.Index(indexName).Query(query));

            return response;
        }
        
        public virtual async Task<CountResponse> CountByExpAsync(
            Expression<Func<TEntity, bool>> filterExp)
        {
            var indexName = IndexName;
            var client = GetElasticClient();
            Func<QueryContainerDescriptor<TEntity>, QueryContainer> query = q => Builders<TEntity>.Filter.Where(filterExp).Query;
            var response = await client.CountAsync<TEntity>(c => c.Index(indexName).Query(query));
            return response;
        }
    }
}