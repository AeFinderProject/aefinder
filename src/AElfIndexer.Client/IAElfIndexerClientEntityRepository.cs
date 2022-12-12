using System.Linq.Expressions;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Nest;

namespace AElfIndexer.Client;

public interface IAElfIndexerClientEntityRepository<TEntity, TKey, TData, T>
    where TEntity : AElfIndexerClientEntity<TKey>, IIndexBuild, new()
    where TData : BlockChainDataBase
{
    Task AddOrUpdateAsync(TEntity entity);
    Task<TEntity> GetFromBlockStateSetAsync(TKey id, string chainId);

    Task<TEntity> GetAsync(TKey id);
    
    Task<TEntity> GetAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending,
        string index = null);

    Task<Tuple<long, List<TEntity>>> GetListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending,
        int limit = 1000,
        int skip = 0,
        string index = null);

    Task<Tuple<long, List<TEntity>>> GetSortListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null,
        int limit = 1000,
        int skip = 0,
        string index = null);

    Task<CountResponse> CountAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> query,
        string indexPrefix = null);
}