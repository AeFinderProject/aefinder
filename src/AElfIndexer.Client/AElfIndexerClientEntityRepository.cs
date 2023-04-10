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
    private readonly IDAppDataProvider _dAppDataProvider;
    private readonly IBlockStateSetProvider<TData> _blockStateSetProvider;
    private readonly IDAppDataIndexProvider<TEntity> _dAppDataIndexProvider;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;

    private readonly string _entityName;

    public AElfIndexerClientEntityRepository(INESTRepository<TEntity, string> nestRepository,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<TData> blockStateSetProvider, IDAppDataIndexProvider<TEntity> dAppDataIndexProvider)
    {
        _nestRepository = nestRepository;
        _dAppDataProvider = dAppDataProvider;
        _blockStateSetProvider = blockStateSetProvider;
        _dAppDataIndexProvider = dAppDataIndexProvider;
        _clientInfoProvider = aelfIndexerClientInfoProvider;

        _entityName = typeof(TEntity).Name;
        // _clientId = aelfIndexerClientInfoProvider.GetClientId();
        // _version = aelfIndexerClientInfoProvider.GetVersion();
        // _indexName = $"{_clientId}-{_version}.{_entityName}".ToLower();
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

    private async Task AddOrUpdateForConfirmBlockAsync(string dataKey, TEntity entity)
    {
        await _dAppDataIndexProvider.AddOrUpdateAsync(entity, GetIndexName());
        await _dAppDataProvider.SetLibValueAsync(dataKey,entity);
    }
    
    private async Task DeleteForConfirmBlockAsync(string dataKey, TEntity entity)
    {
        await _dAppDataIndexProvider.DeleteAsync(entity, GetIndexName());
        await _dAppDataProvider.SetLibValueAsync(dataKey,entity);
    }

    private async Task OperationAsync(TEntity entity, Func<string, TEntity,Task> confirmBlockFunc,
        Func<string, BlockStateSet<TData>, string, TEntity,Task> unConfirmBlockFunc)
    {
        if (!IsValidate(entity)) throw new Exception($"Invalid entity: {entity.ToJsonString()}");
        var entityKey = $"{_entityName}-{entity.Id}";
        
        var blockStateSetsGrainKey = GetBlockStateSetKey(entity.ChainId);
        var dataKey = GetDAppDataKey(entity.ChainId, entityKey);
        
        var dataValue = await _dAppDataProvider.GetLibValueAsync<TEntity>(dataKey);
        var blockStateSets = await _blockStateSetProvider.GetBlockStateSetsAsync(blockStateSetsGrainKey);
        var blockStateSet = blockStateSets[entity.BlockHash];
        // Entity is confirmed,save it to es search directly
        if (blockStateSet.Confirmed)
        {
            if ((dataValue?.BlockHeight??0) > blockStateSet.BlockHeight)
            {
                return;
            }

            await confirmBlockFunc(dataKey, entity);
            return;
        }
        //Deal with fork
        var longestChainHashes = await _blockStateSetProvider.GetLongestChainHashesAsync(blockStateSetsGrainKey);
        // entity is on best chain
        if (longestChainHashes.ContainsKey(entity.BlockHash))
        {
            await unConfirmBlockFunc(blockStateSetsGrainKey, blockStateSet, entityKey, entity);
        }
        else // entity is not on best chain.
        {
            //if current block state is not on best chain, get the best chain block state set
            var longestChainBlockStateSet = await _blockStateSetProvider.GetLongestChainBlockStateSetAsync(blockStateSetsGrainKey);
            var entityFromBlockStateSet = GetEntityFromBlockStateSets(entityKey, blockStateSets, longestChainBlockStateSet.BlockHash,
                longestChainBlockStateSet.BlockHeight, dataValue);
            if (entityFromBlockStateSet != null)
            {
                await _dAppDataIndexProvider.AddOrUpdateAsync(entityFromBlockStateSet, GetIndexName());
                // await dappGrain.SetLatestValue(entityFromBlockStateSet);
            }
            else
            {
                //if block state set has entityKey, use it to set entity.
                if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
                {
                    entity = JsonConvert.DeserializeObject<TEntity>(value);
                }
                
                await _dAppDataIndexProvider.DeleteAsync(entity, GetIndexName());
                entity.IsDeleted = true;
                await _dAppDataProvider.SetLibValueAsync<TEntity>(dataKey, entity);
            }
        }
    }

    public async Task<TEntity> GetFromBlockStateSetAsync(string id, string chainId)
    {
        var entityKey = $"{_entityName}-{id}";
        var dateKey = GetDAppDataKey(chainId, entityKey);
        var blockStateSetsKey = GetBlockStateSetKey(chainId);
        
        var entity = await _dAppDataProvider.GetLibValueAsync<TEntity>(dateKey);

        var blockStateSets = await _blockStateSetProvider.GetBlockStateSetsAsync(blockStateSetsKey);
        
        var currentBlockStateSet = await _blockStateSetProvider.GetCurrentBlockStateSetAsync(blockStateSetsKey);
        return GetEntityFromBlockStateSets(entityKey, blockStateSets, currentBlockStateSet.BlockHash,
            currentBlockStateSet.BlockHeight, entity);
    }

    public async Task<TEntity> GetAsync(string id)
    {
        return await _nestRepository.GetAsync(id,GetIndexName());
    }

    public async Task<TEntity> GetAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending)
    {
        return await _nestRepository.GetAsync(filterFunc, includeFieldFunc, sortExp, sortType, GetIndexName());
    }

    public async Task<Tuple<long, List<TEntity>>> GetListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending,
        int limit = 1000,
        int skip = 0)
    {
        return await _nestRepository.GetListAsync(filterFunc, includeFieldFunc, sortExp, sortType, limit, skip, GetIndexName());
    }

    public async Task<Tuple<long, List<TEntity>>> GetSortListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null,
        int limit = 1000,
        int skip = 0)
    {
        return await _nestRepository.GetSortListAsync(filterFunc, includeFieldFunc, sortFunc, limit, skip, GetIndexName());
    }

    public async Task<CountResponse> CountAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> query)
    {
        return await _nestRepository.CountAsync(query, GetIndexName());
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
        await _dAppDataIndexProvider.AddOrUpdateAsync(entity, GetIndexName());
        await _blockStateSetProvider.SetBlockStateSetAsync(blockStateSetKey,blockStateSet);
    }
    
    private async Task RemoveFromBlockStateSetAsync(string blockStateSetKey, BlockStateSet<TData> blockStateSet, string entityKey,TEntity entity)
    {
        entity.IsDeleted = true;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        await _dAppDataIndexProvider.DeleteAsync(entity, GetIndexName());
        await _blockStateSetProvider.SetBlockStateSetAsync(blockStateSetKey, blockStateSet);
    }
    
    private TEntity GetEntityFromBlockStateSets(string entityKey, Dictionary<string, BlockStateSet<TData>> blockStateSets, string currentBlockHash, long currentBlockHeight, TEntity libValue)
    {
        var blockHash = currentBlockHash;
        while (blockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
            {
                var entity = JsonConvert.DeserializeObject<TEntity>(value);
                return (entity?.IsDeleted ?? true) ? null : entity;
            }

            blockHash = blockStateSet.PreviousBlockHash;
        }

        // if block state sets don't contain entity, return LIB value
        // lib value's block height should less than min block state set's block height.
        return libValue != null && libValue.BlockHeight <= currentBlockHeight && !libValue.IsDeleted ? libValue : null;
    }

    private string GetIndexName()
    {
        var clientId = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();
        return $"{clientId}-{version}.{_entityName}".ToLower();
    }

    private string GetDAppDataKey(string chainId, string entityKey)
    {
        var clientId = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();
        return GrainIdHelper.GenerateGrainId("DAppData", clientId, chainId, version, entityKey);
    }
    
    private string GetBlockStateSetKey(string chainId)
    {
        var clientId = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();
        return GrainIdHelper.GenerateGrainId("BlockStateSets", clientId, chainId, version);
    }
}