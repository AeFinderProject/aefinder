using System.Linq.Expressions;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Nest;
using Newtonsoft.Json;
using Orleans;

namespace AElfIndexer.Client;

public class AElfIndexerClientEntityRepository<TEntity,TKey,TData,T> : IAElfIndexerClientEntityRepository<TEntity,TKey,TData,T>
    where TEntity: AElfIndexerClientEntity<TKey>, new()
    where TData : BlockChainDataBase
{
    private readonly INESTRepository<TEntity, TKey> _nestRepository;
    private readonly IClusterClient _clusterClient;
    private readonly string _entityName;
    private readonly IClientInfoProvider<T> _clientInfoProvider;

    public AElfIndexerClientEntityRepository(INESTRepository<TEntity, TKey> nestRepository,
        IClusterClient clusterClient, IClientInfoProvider<T> clientInfoProvider)
    {
        _nestRepository = nestRepository;
        _clusterClient = clusterClient;
        _clientInfoProvider = clientInfoProvider;
        _entityName = typeof(TEntity).Name;
    }

    public async Task AddOrUpdateAsync(TEntity entity)
    {
        if (!IsValidate(entity)) throw new Exception($"Invalid entity: {entity.ToJsonString()}");
        var clientId = _clientInfoProvider.GetClientId();
        var indexPrefix = _clientInfoProvider.GetIndexPrefixes()[entity.ChainId];
        var entityKey = $"{_entityName}_{entity.Id}";
        //TODO 统一GrainId的格式
        var blockStateSetsGrainKey = $"BlockStateSets_{entity.ChainId}_{clientId}";
        var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>(blockStateSetsGrainKey);
        var dappGrain = _clusterClient.GetGrain<IDappDataGrain<TEntity>>(
            $"DappData_{entity.ChainId}_{clientId}_{indexPrefix}_{entityKey}");
        var dataValue = await dappGrain.GetValue();
        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        var blockStateSet = blockStateSets[entity.BlockHash];
        // Entity is confirmed,save it to es search directly
        if (entity.IsConfirmed)
        {
            if ((dataValue.LIBValue?.BlockHeight??0) >= blockStateSet.BlockHeight) return;
            // Use value in BlockStateSet to override confirmed entity value
            if (!blockStateSet.Changes.TryGetValue(entityKey, out var value))
                throw new Exception($"{blockStateSetsGrainKey} does not contain {entityKey}");
            entity = JsonConvert.DeserializeObject<TEntity>(value);
            //$"{IndexSettingOptions.IndexPrefix.ToLower()}.{typeof(TEntity).Name.ToLower()}"
            await _nestRepository.AddOrUpdateAsync(entity, $"{clientId}{indexPrefix}.{_entityName}".ToLower());
            await dappGrain.SetLIBValue(entity);
            return;
        }
        // No fork, save it to es search directly
        if (!await blockStateSetsGrain.HasFork())
        {
            await TryAddToBlockStateSetAsync(blockStateSet, entityKey, entity, dappGrain, blockStateSetsGrain);
            return;
        }
        //Deal with fork
        var bestChainHashes = await blockStateSetsGrain.GetBestChainHashes();
        // entity is on best chain
        if (bestChainHashes.ContainsKey(entity.BlockHash))
        {
            await TryAddToBlockStateSetAsync(blockStateSet, entityKey, entity, dappGrain, blockStateSetsGrain);
        }
        else // entity is not on best chain.
        {
            //if block state set has entityKey, use it to set entity.
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
                entity = JsonConvert.DeserializeObject<TEntity>(value);
            // if latest value == entity, need to update es search and latest value
            if (entity != null && entity.ToJsonString() == dataValue.LatestValue?.ToJsonString())
            {
                //if current block state is not on best chain, get the best chain block state set
                var bestChainBlockStateSet = blockStateSets.First(b =>
                    b.Value.BlockHeight == entity.BlockHeight &&
                    bestChainHashes.ContainsKey(b.Value.BlockHash)).Value;
                var entityFromBlockStateSet = GetEntityFromBlockStateSets(entityKey, blockStateSets, bestChainBlockStateSet.BlockHash,
                    bestChainBlockStateSet.BlockHeight, dataValue.LIBValue);
                if (entityFromBlockStateSet != null)
                {
                    await _nestRepository.AddOrUpdateAsync(entityFromBlockStateSet, $"{clientId}{indexPrefix}.{_entityName}".ToLower());
                    await dappGrain.SetLatestValue(entityFromBlockStateSet);
                }
                else
                {
                    await _nestRepository.DeleteAsync(entity, $"{clientId}{indexPrefix}.{_entityName}".ToLower());
                    await dappGrain.SetLIBValue(null);
                    await dappGrain.SetLatestValue(null);
                }
            }
        }
    }
    
    public async Task<TEntity> GetAsync(TKey id, string chainId)
    {
        var clientId = _clientInfoProvider.GetClientId();
        var indexPrefix = _clientInfoProvider.GetIndexPrefixes()[chainId];
        var entityKey = $"{_entityName}_{id}";
        var dappGrain = _clusterClient.GetGrain<IDappDataGrain<TEntity>>(
            $"DappData_{chainId}_{clientId}_{indexPrefix}_{entityKey}");
        var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>($"BlockStateSets_{chainId}_{clientId}");
        var entity = await dappGrain.GetValue();
        // Do not have fork, just return latest value
        if (!await blockStateSetsGrain.HasFork())
        {
            return entity.LatestValue;
        }

        // Has fork, get value from block state sets first.
        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        
        var currentBlockStateSet = await blockStateSetsGrain.GetCurrentBlockStateSet();
        return GetEntityFromBlockStateSets(entityKey, blockStateSets, currentBlockStateSet.BlockHash,
            currentBlockStateSet.BlockHeight, entity.LIBValue);
    }

    public async Task<TEntity> GetAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending,
        string index = null)
    {
        return await _nestRepository.GetAsync(filterFunc, includeFieldFunc, sortExp, sortType, index);
    }

    public async Task<Tuple<long, List<TEntity>>> GetListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Expression<Func<TEntity, object>> sortExp = null,
        SortOrder sortType = SortOrder.Ascending,
        int limit = 1000,
        int skip = 0,
        string index = null)
    {
        return await _nestRepository.GetListAsync(filterFunc, includeFieldFunc, sortExp, sortType, limit, skip, index);
    }

    public async Task<Tuple<long, List<TEntity>>> GetSortListAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> filterFunc = null,
        Func<SourceFilterDescriptor<TEntity>, ISourceFilter> includeFieldFunc = null,
        Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sortFunc = null,
        int limit = 1000,
        int skip = 0,
        string index = null)
    {
        return await _nestRepository.GetSortListAsync(filterFunc, includeFieldFunc, sortFunc, limit, skip, index);
    }

    public async Task<CountResponse> CountAsync(
        Func<QueryContainerDescriptor<TEntity>, QueryContainer> query,
        string indexPrefix = null)
    {
        return await _nestRepository.CountAsync(query, indexPrefix);
    }
    
    private bool IsValidate(TEntity entity)
    {
        return !string.IsNullOrWhiteSpace(entity.BlockHash) && entity.BlockHeight != 0 && entity.Id != null &&
               !string.IsNullOrWhiteSpace(entity.ChainId) && !string.IsNullOrWhiteSpace(entity.PreviousBlockHash);
    }

    private async Task<bool> TryAddToBlockStateSetAsync(BlockStateSet<TData> blockStateSet, string entityKey, TEntity entity, IDappDataGrain<TEntity> dappGrain, IBlockStateSetsGrain<TData> blockStateSetsGrain)
    {
        var clientId = _clientInfoProvider.GetClientId();
        var indexPrefix = _clientInfoProvider.GetIndexPrefixes()[entity.ChainId];
        if (blockStateSet.Changes.TryGetValue(entityKey, out _)) return false;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        //$"{IndexSettingOptions.IndexPrefix.ToLower()}.{typeof(TEntity).Name.ToLower()}"
        await _nestRepository.AddOrUpdateAsync(entity, $"{clientId}{indexPrefix}.{_entityName}".ToLower());
        await dappGrain.SetLatestValue(entity);
        await blockStateSetsGrain.SetBlockStateSet(blockStateSet);
        return true;
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
        return libValue.BlockHeight < currentBlockHeight ? libValue : null;
    }
}