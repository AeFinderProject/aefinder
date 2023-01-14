using System.Linq.Expressions;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client;

public class AElfIndexerClientEntityRepository<TEntity,TData> : IAElfIndexerClientEntityRepository<TEntity,TData>
    where TEntity : AElfIndexerClientEntity<string>, IIndexBuild, new()
    where TData : BlockChainDataBase
{
    private readonly INESTRepository<TEntity, string> _nestRepository;
    //private readonly IClusterClient _clusterClient;
    private readonly string _entityName;
    private readonly string _clientId;
    private readonly string _version;
    private readonly string _indexName; 
    
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    protected IDAppDataProvider DAppDataProvider => this.LazyServiceProvider.LazyGetRequiredService<IDAppDataProvider>();
    protected IBlockStateSetProvider<TData> BlockStateSetProvider => this.LazyServiceProvider.LazyGetRequiredService<IBlockStateSetProvider<TData>>();

    public AElfIndexerClientEntityRepository(INESTRepository<TEntity, string> nestRepository,
        IClusterClient clusterClient, IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider)
    {
        _nestRepository = nestRepository;
        //_clusterClient = clusterClient;
        _entityName = typeof(TEntity).Name;
        _clientId = aelfIndexerClientInfoProvider.GetClientId();
        _version = aelfIndexerClientInfoProvider.GetVersion();
        _indexName = $"{_clientId}-{_version}.{_entityName}".ToLower();
    }

    public async Task AddOrUpdateAsync(TEntity entity)
    {
        entity.IsDeleted = false;
        await OperationAsync(entity, AddOrUpdateForConfirmBlockAsync, AddToBlockStateSetAsync);
    }
    
    public async Task DeleteAsync(TEntity entity)
    {
        entity.IsDeleted = true;
        await OperationAsync(entity, DeleteForConfirmBlockAsync, RemoveFromBlockStateSetAsync);
    }

    private async Task AddOrUpdateForConfirmBlockAsync(string dappDataKey, TEntity entity)
    {
        await _nestRepository.AddOrUpdateAsync(entity, _indexName);
        await DAppDataProvider.SetLibValueAsync(dappDataKey,entity);
    }
    
    private async Task DeleteForConfirmBlockAsync(string dappDataKey, TEntity entity)
    {
        await _nestRepository.DeleteAsync(entity, _indexName);
        await DAppDataProvider.SetLibValueAsync(dappDataKey,entity);
    }

    private async Task OperationAsync(TEntity entity, Func<string, TEntity,Task> confirmBlockFunc,
        Func<string, BlockStateSet<TData>, string, TEntity,Task> unConfirmBlockFunc)
    {
        if (!IsValidate(entity)) throw new Exception($"Invalid entity: {entity.ToJsonString()}");
        var entityKey = $"{_entityName}-{entity.Id}";
        
        var blockStateSetsGrainKey =
            GrainIdHelper.GenerateGrainId("BlockStateSets", _clientId, entity.ChainId, _version);
        var dappDataKey = GrainIdHelper.GenerateGrainId("DAppData", _clientId, entity.ChainId, _version, entityKey);
        
        var dataValue = await DAppDataProvider.GetLibValueAsync<TEntity>(dappDataKey);
        var blockStateSets = await BlockStateSetProvider.GetBlockStateSets(blockStateSetsGrainKey);
        var blockStateSet = blockStateSets[entity.BlockHash];
        // Entity is confirmed,save it to es search directly
        if (blockStateSet.Confirmed)
        {
            if ((dataValue?.BlockHeight??0) >= blockStateSet.BlockHeight) return;

            await confirmBlockFunc(dappDataKey, entity);
            return;
        }
        //Deal with fork
        var longestChainHashes = await BlockStateSetProvider.GetLongestChainHashes(blockStateSetsGrainKey);
        // entity is on best chain
        if (longestChainHashes.ContainsKey(entity.BlockHash))
        {
            await unConfirmBlockFunc(blockStateSetsGrainKey, blockStateSet, entityKey, entity);
        }
        else // entity is not on best chain.
        {
            //if current block state is not on best chain, get the best chain block state set
            var longestChainBlockStateSet = await BlockStateSetProvider.GetLongestChainBlockStateSet(blockStateSetsGrainKey);
            var entityFromBlockStateSet = GetEntityFromBlockStateSets(entityKey, blockStateSets, longestChainBlockStateSet.BlockHash,
                longestChainBlockStateSet.BlockHeight, dataValue);
            if (entityFromBlockStateSet != null)
            {
                await _nestRepository.AddOrUpdateAsync(entityFromBlockStateSet, _indexName);
                // await dappGrain.SetLatestValue(entityFromBlockStateSet);
            }
            else
            {
                //if block state set has entityKey, use it to set entity.
                if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
                {
                    entity = JsonConvert.DeserializeObject<TEntity>(value);
                }
                
                await _nestRepository.DeleteAsync(entity, _indexName);
                entity.IsDeleted = true;
                await DAppDataProvider.SetLibValueAsync<TEntity>(dappDataKey, entity);
            }
        }
    }

    // TODO: Need it?
    public async Task<TEntity> GetFromBlockStateSetAsync(string id, string chainId)
    {
        var entityKey = $"{_entityName}-{id}";
        var dappKey = GrainIdHelper.GenerateGrainId("DAppData", _clientId, chainId, _version, entityKey);
        var blockStateSetsKey = GrainIdHelper.GenerateGrainId("BlockStateSets", _clientId, chainId, _version);
        
        var entity = await DAppDataProvider.GetLibValueAsync<TEntity>(dappKey);

        var blockStateSets = await BlockStateSetProvider.GetBlockStateSets(blockStateSetsKey);
        
        var currentBlockStateSet = await BlockStateSetProvider.GetCurrentBlockStateSet(blockStateSetsKey);
        return GetEntityFromBlockStateSets(entityKey, blockStateSets, currentBlockStateSet.BlockHash,
            currentBlockStateSet.BlockHeight, entity);
    }

    public async Task<TEntity> GetAsync(string id)
    {
        return await _nestRepository.GetAsync(id,_indexName);
    }

    public async Task<TEntity> GetAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending)
    {
        return await _nestRepository.GetAsync(filterFunc, includeFieldFunc, sortExp, sortType, _indexName);
    }

    public async Task<Tuple<long, List<TEntity>>> GetListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending,
        int limit = 1000,
        int skip = 0)
    {
        return await _nestRepository.GetListAsync(filterFunc, includeFieldFunc, sortExp, sortType, limit, skip, _indexName);
    }

    public async Task<Tuple<long, List<TEntity>>> GetSortListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null,
        int limit = 1000,
        int skip = 0)
    {
        return await _nestRepository.GetSortListAsync(filterFunc, includeFieldFunc, sortFunc, limit, skip, _indexName);
    }

    public async Task<CountResponse> CountAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> query)
    {
        return await _nestRepository.CountAsync(query, _indexName);
    }
    
    private bool IsValidate(TEntity entity)
    {
        return !string.IsNullOrWhiteSpace(entity.BlockHash) && entity.BlockHeight != 0 && entity.Id != null &&
               !string.IsNullOrWhiteSpace(entity.ChainId) && !string.IsNullOrWhiteSpace(entity.PreviousBlockHash);
    }

    private async Task AddToBlockStateSetAsync(string blockStateSetKey, BlockStateSet<TData> blockStateSet, string entityKey, TEntity entity)
    {
        entity.IsDeleted = false;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        await _nestRepository.AddOrUpdateAsync(entity, _indexName);
        await BlockStateSetProvider.SetBlockStateSet(blockStateSetKey,blockStateSet);
    }
    
    private async Task RemoveFromBlockStateSetAsync(string blockStateSetKey, BlockStateSet<TData> blockStateSet, string entityKey,TEntity entity)
    {
        entity.IsDeleted = true;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        await _nestRepository.DeleteAsync(entity, _indexName);
        await BlockStateSetProvider.SetBlockStateSet(blockStateSetKey, blockStateSet);
    }
    
    private TEntity GetEntityFromBlockStateSets(string entityKey, Dictionary<string, BlockStateSet<TData>> blockStateSets, string currentBlockHash, long currentBlockHeight, TEntity libValue)
    {
        while (blockStateSets.TryGetValue(currentBlockHash, out var blockStateSet))
        {
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
            {
                var entity = JsonConvert.DeserializeObject<TEntity>(value);
                return (entity?.IsDeleted ?? true) ? null : entity;
            }

            currentBlockHash = blockStateSet.PreviousBlockHash;
            currentBlockHeight = blockStateSet.BlockHeight;
        }

        // if block state sets don't contain entity, return LIB value
        // lib value's block height should less than min block state set's block height.
        return libValue != null && libValue.BlockHeight < currentBlockHeight ? libValue : null;
    }
}