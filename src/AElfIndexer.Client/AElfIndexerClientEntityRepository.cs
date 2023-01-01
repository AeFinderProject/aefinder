using System.Linq.Expressions;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Nest;
using Newtonsoft.Json;
using Orleans;

namespace AElfIndexer.Client;

public class AElfIndexerClientEntityRepository<TEntity,TData> : IAElfIndexerClientEntityRepository<TEntity,TData>
    where TEntity : AElfIndexerClientEntity<string>, IIndexBuild, new()
    where TData : BlockChainDataBase
{
    private readonly INESTRepository<TEntity, string> _nestRepository;
    private readonly IClusterClient _clusterClient;
    private readonly string _entityName;
    private readonly string _clientId;
    private readonly string _version;
    private readonly string _indexName; 

    public AElfIndexerClientEntityRepository(INESTRepository<TEntity, string> nestRepository,
        IClusterClient clusterClient, IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider)
    {
        _nestRepository = nestRepository;
        _clusterClient = clusterClient;
        _entityName = typeof(TEntity).Name;
        _clientId = aelfIndexerClientInfoProvider.GetClientId();
        _version = aelfIndexerClientInfoProvider.GetVersion();
        _indexName = $"{_clientId}-{_version}.{_entityName}".ToLower();
    }

    public async Task AddOrUpdateAsync(TEntity entity)
    {
        await OperationAsync(entity, AddOrUpdateForConfirmBlockAsync, AddToBlockStateSetAsync);
    }
    
    public async Task DeleteAsync(TEntity entity)
    {
        await OperationAsync(entity, DeleteForConfirmBlockAsync, RemoveFromBlockStateSetAsync);
    }

    private async Task AddOrUpdateForConfirmBlockAsync(IDappDataGrain dataGrain, TEntity entity)
    {
        await _nestRepository.AddOrUpdateAsync(entity, _indexName);
        await dataGrain.SetLIBValue(entity);
    }
    
    private async Task DeleteForConfirmBlockAsync(IDappDataGrain dataGrain, TEntity entity)
    {
        await _nestRepository.DeleteAsync(entity, _indexName);
        await dataGrain.SetLIBValue(null);
    }

    private async Task OperationAsync(TEntity entity, Func<IDappDataGrain, TEntity,Task> confirmBlockFunc,
        Func<BlockStateSet<TData>, string, TEntity, IBlockStateSetsGrain<TData>,Task> unConfirmBlockFunc)
    {
        if (!IsValidate(entity)) throw new Exception($"Invalid entity: {entity.ToJsonString()}");
        var entityKey = $"{_entityName}-{entity.Id}";
        
        var blockStateSetsGrainKey =
            GrainIdHelper.GenerateGrainId("BlockStateSets", _clientId, entity.ChainId, _version);
        var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>(blockStateSetsGrainKey);
        var dappGrain =
            _clusterClient.GetGrain<IDappDataGrain>(GrainIdHelper.GenerateGrainId("DappData", _clientId, entity.ChainId,
                _version, entityKey));
        
        var dataValue = await dappGrain.GetValue<TEntity>();
        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        var blockStateSet = blockStateSets[entity.BlockHash];
        // Entity is confirmed,save it to es search directly
        if (blockStateSet.Confirmed)
        {
            if ((dataValue.LIBValue?.BlockHeight??0) >= blockStateSet.BlockHeight) return;

            await confirmBlockFunc(dappGrain, entity);
            return;
        }
        //Deal with fork
        var longestChainHashes = await blockStateSetsGrain.GetLongestChainHashes();
        // entity is on best chain
        if (longestChainHashes.ContainsKey(entity.BlockHash))
        {
            await unConfirmBlockFunc(blockStateSet, entityKey, entity, blockStateSetsGrain);
        }
        else // entity is not on best chain.
        {
            //if block state set has entityKey, use it to set entity.
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
            {
                entity = JsonConvert.DeserializeObject<TEntity>(value);
            }
                
            //if current block state is not on best chain, get the best chain block state set
            var longestChainBlockStateSet = await blockStateSetsGrain.GetLongestChainBlockStateSet();
            var entityFromBlockStateSet = GetEntityFromBlockStateSets(entityKey, blockStateSets, longestChainBlockStateSet.BlockHash,
                longestChainBlockStateSet.BlockHeight, dataValue.LIBValue);
            if (entityFromBlockStateSet != null)
            {
                await _nestRepository.AddOrUpdateAsync(entityFromBlockStateSet, _indexName);
                // await dappGrain.SetLatestValue(entityFromBlockStateSet);
            }
            else
            {
                await _nestRepository.DeleteAsync(entity, _indexName);
                await dappGrain.SetLIBValue(null);
            }
        }
    }

    public async Task<TEntity> GetFromBlockStateSetAsync(string id, string chainId)
    {
        var entityKey = $"{_entityName}-{id}";
        var dappGrain = _clusterClient.GetGrain<IDappDataGrain>(
            GrainIdHelper.GenerateGrainId("DappData", _clientId, chainId, _version, entityKey));
        var blockStateSetsGrain =
            _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", _clientId, chainId, _version));
        
        var entity = await dappGrain.GetValue<TEntity>();

        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        
        var currentBlockStateSet = await blockStateSetsGrain.GetCurrentBlockStateSet();
        return GetEntityFromBlockStateSets(entityKey, blockStateSets, currentBlockStateSet.BlockHash,
            currentBlockStateSet.BlockHeight, entity.LIBValue);
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

    private async Task AddToBlockStateSetAsync(BlockStateSet<TData> blockStateSet, string entityKey, TEntity entity, IBlockStateSetsGrain<TData> blockStateSetsGrain)
    {
        if (blockStateSet.Changes.TryGetValue(entityKey, out _)) return;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();

        await _nestRepository.AddOrUpdateAsync(entity, _indexName);
        await blockStateSetsGrain.SetBlockStateSet(blockStateSet);
    }
    
    private async Task RemoveFromBlockStateSetAsync(BlockStateSet<TData> blockStateSet, string entityKey,TEntity entity, IBlockStateSetsGrain<TData> blockStateSetsGrain)
    {
        if (blockStateSet.Changes.TryGetValue(entityKey, out _)) return;
        blockStateSet.Changes.Remove(entityKey);

        await _nestRepository.DeleteAsync(entity, _indexName);
        await blockStateSetsGrain.SetBlockStateSet(blockStateSet);
    }
    
    private TEntity GetEntityFromBlockStateSets(string entityKey, Dictionary<string, BlockStateSet<TData>> blockStateSets, string currentBlockHash, long currentBlockHeight, TEntity libValue)
    {
        while (blockStateSets.TryGetValue(currentBlockHash, out var blockStateSet))
        {
            if (blockStateSet.Changes.TryGetValue(entityKey, out var entity))
            {
                return JsonConvert.DeserializeObject<TEntity>(entity);
            }

            currentBlockHash = blockStateSet.PreviousBlockHash;
            currentBlockHeight = blockStateSet.BlockHeight;
        }

        // if block state sets don't contain entity, return LIB value
        // lib value's block height should less than min block state set's block height.
        return libValue != null && libValue.BlockHeight < currentBlockHeight ? libValue : null;
    }
}