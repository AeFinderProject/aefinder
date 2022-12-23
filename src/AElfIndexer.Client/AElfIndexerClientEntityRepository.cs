using System.Linq.Expressions;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Nest;
using Newtonsoft.Json;
using Orleans;

namespace AElfIndexer.Client;

public class AElfIndexerClientEntityRepository<TEntity,TKey,TData,T> : IAElfIndexerClientEntityRepository<TEntity,TKey,TData,T>
    where TEntity : AElfIndexerClientEntity<TKey>, IIndexBuild, new()
    where TData : BlockChainDataBase
{
    private readonly INESTRepository<TEntity, TKey> _nestRepository;
    private readonly IClusterClient _clusterClient;
    private readonly string _entityName;
    private readonly string _clientId;
    private readonly string _version;
    private readonly string _indexName; 

    public AElfIndexerClientEntityRepository(INESTRepository<TEntity, TKey> nestRepository,
        IClusterClient clusterClient, IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider)
    {
        _nestRepository = nestRepository;
        _clusterClient = clusterClient;
        _entityName = typeof(TEntity).Name;
        _clientId = aelfIndexerClientInfoProvider.GetClientId();
        _version = aelfIndexerClientInfoProvider.GetVersion();
        _indexName = $"{_clientId}_{_version}.{_entityName}".ToLower();
    }

    public async Task AddOrUpdateAsync(TEntity entity)
    {
        if (!IsValidate(entity)) throw new Exception($"Invalid entity: {entity.ToJsonString()}");
        var entityKey = $"{_entityName}_{entity.Id}";
        //TODO 统一GrainId的格式
        var blockStateSetsGrainKey = $"BlockStateSets_{_clientId}_{entity.ChainId}_{_version}";
        var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>(blockStateSetsGrainKey);
        var dappGrain = _clusterClient.GetGrain<IDappDataGrain>(
            $"DappData_{_clientId}_{entity.ChainId}_{_version}_{entityKey}");
        var dataValue = await dappGrain.GetValue<TEntity>();
        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        var blockStateSet = blockStateSets[entity.BlockHash];
        // Entity is confirmed,save it to es search directly
        if (blockStateSet.Confirmed)
        {
            if ((dataValue.LIBValue?.BlockHeight??0) >= blockStateSet.BlockHeight) return;
            
            await _nestRepository.AddOrUpdateAsync(entity, _indexName);
            await dappGrain.SetLIBValue(entity);
            return;
        }
        //Deal with fork
        var longestChainHashes = await blockStateSetsGrain.GetLongestChainHashes();
        // entity is on best chain
        if (longestChainHashes.ContainsKey(entity.BlockHash))
        {
            await TryAddToBlockStateSetAsync(blockStateSet, entityKey, entity, dappGrain, blockStateSetsGrain);
        }
        else // entity is not on best chain.
        {
            //if block state set has entityKey, use it to set entity.
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
            {
                entity = JsonConvert.DeserializeObject<TEntity>(value);
            }
                
            //if current block state is not on best chain, get the best chain block state set
            var bestChainBlockStateSet = await blockStateSetsGrain.GetBestChainBlockStateSet();
            var entityFromBlockStateSet = GetEntityFromBlockStateSets(entityKey, blockStateSets, bestChainBlockStateSet.BlockHash,
                bestChainBlockStateSet.BlockHeight, dataValue.LIBValue);
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
    
    public async Task<TEntity> GetFromBlockStateSetAsync(TKey id, string chainId)
    {
        var entityKey = $"{_entityName}_{id}";
        var dappGrain = _clusterClient.GetGrain<IDappDataGrain>(
            $"DappData_{_clientId}_{chainId}_{_version}_{entityKey}");
        var blockStateSetsGrain =
            _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>(
                $"BlockStateSets_{_clientId}_{chainId}_{_version}");
        var entity = await dappGrain.GetValue<TEntity>();

        // Has fork, get value from block state sets first.
        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        
        var currentBlockStateSet = await blockStateSetsGrain.GetCurrentBlockStateSet();
        return GetEntityFromBlockStateSets(entityKey, blockStateSets, currentBlockStateSet.BlockHash,
            currentBlockStateSet.BlockHeight, entity.LIBValue);
    }

    public async Task<TEntity> GetAsync(TKey id)
    {
        return await _nestRepository.GetAsync(id,_indexName);
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

    private async Task<bool> TryAddToBlockStateSetAsync(BlockStateSet<TData> blockStateSet, string entityKey, TEntity entity, IDappDataGrain dappGrain, IBlockStateSetsGrain<TData> blockStateSetsGrain)
    {
        if (blockStateSet.Changes.TryGetValue(entityKey, out _)) return false;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        //$"{IndexSettingOptions.IndexPrefix.ToLower()}.{typeof(TEntity).Name.ToLower()}"
        await _nestRepository.AddOrUpdateAsync(entity, $"{_clientId}{_version}.{_entityName}".ToLower());
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