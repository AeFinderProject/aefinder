using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nest;
using Volo.Abp.Domain.Entities;

namespace AElf.Indexing.Elasticsearch
{
    public interface INESTReaderRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>, new()
    {
        Task<TEntity> GetAsync(TKey id);

        Task<TEntity> GetAsync(Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
            Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
            Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending);

        // Task<TEntity> GetByExpAsync(Expression<Func<TEntity, bool>> filterExp = null,
        //     Expression<Func<TEntity, object>> includeFieldExp = null,
        //     Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending);
        
        // Task<Tuple<long, List<TEntity>>> GetListByExpAsync(Expression<Func<TEntity, bool>> filterExp = null,
        //     Expression<Func<TEntity, object>> includeFieldExp = null,
        //     Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending
        //     , int limit = 10, int skip = 0);
        
        Task<Tuple<long, List<TEntity>>> GetListAsync(
            Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
            Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
            Expression<Func<TEntity, object>> sortExp = null, SortOrder sortType = SortOrder.Ascending
            , int limit = 1000, int skip = 0);
        
        Task<ISearchResponse<TEntity>> SearchAsync(SearchDescriptor<TEntity> query,
            int skip, int size, string[] includeFields = null,
            string preTags = "<strong style=\"color: red;\">", string postTags = "</strong>", bool disableHigh = false,
            params string[] highField);

        Task<CountResponse> CountAsync(
            Func<QueryContainerDescriptor<TEntity>, QueryContainer> query);
        // Task<CountResponse> CountByExpAsync(
        //     Expression<Func<TEntity, bool>> filterExp);
    }
}