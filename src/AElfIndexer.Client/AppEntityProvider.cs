using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.State.Client;
using AElfIndexer.Sdk;
using Newtonsoft.Json;

namespace AElfIndexer.Client;

public class AppEntityProvider<TEntity> : IAppEntityProvider<TEntity>
    where TEntity : IndexerEntity, IIndexerEntity
{
    private readonly IBlockProcessingContext _blockProcessingContext;
    private readonly IDAppDataProvider _dAppDataProvider;
    private readonly IBlockStateSetProvider _blockStateSetProvider;
    private readonly IDAppDataIndexProvider<TEntity> _dAppDataIndexProvider;

    private readonly string _entityName;
    
    public AppEntityProvider(IBlockProcessingContext blockProcessingContext, IDAppDataProvider dAppDataProvider, IBlockStateSetProvider blockStateSetProvider, IDAppDataIndexProvider<TEntity> dAppDataIndexProvider)
    {
        _blockProcessingContext = blockProcessingContext;
        _dAppDataProvider = dAppDataProvider;
        _blockStateSetProvider = blockStateSetProvider;
        _dAppDataIndexProvider = dAppDataIndexProvider;
        _entityName = typeof(TEntity).Name;
    }

    public async Task<TEntity> GetAsync(string id)
    {
        var entityKey = $"{_entityName}-{id}";
        var dateKey = GetAppDataKey(entityKey);
        var blockStateSetsKey = GetBlockStateSetKey();
        
        var entity = await _dAppDataProvider.GetLibValueAsync<TEntity>(dateKey);

        var blockStateSets = await _blockStateSetProvider.GetBlockStateSetsAsync(blockStateSetsKey);
        
        var currentBlockStateSet = await _blockStateSetProvider.GetCurrentBlockStateSetAsync(blockStateSetsKey);
        return GetEntityFromBlockStateSets(entityKey, blockStateSets, currentBlockStateSet.Block.BlockHash,
            currentBlockStateSet.Block.BlockHeight, entity);
    }

    public async Task AddOrUpdateAsync(TEntity entity)
    {
        entity.IsDeleted = false;
        await OperationAsync(entity, AddOrUpdateForConfirmBlockAsync, AddToBlockStateSetAsync);
    }
    
    private async Task AddOrUpdateForConfirmBlockAsync(string dataKey, TEntity entity)
    {
        await _dAppDataIndexProvider.AddOrUpdateAsync(entity, GetIndexName());
        await _dAppDataProvider.SetLibValueAsync(dataKey,entity);
    }
    
    private TEntity GetEntityFromBlockStateSets(string entityKey, Dictionary<string, AppBlockStateSet> blockStateSets, string currentBlockHash, long currentBlockHeight, TEntity libValue)
    {
        var blockHash = currentBlockHash;
        while (blockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
            {
                var entity = JsonConvert.DeserializeObject<TEntity>(value);
                return (entity?.IsDeleted ?? true) ? null : entity;
            }

            blockHash = blockStateSet.Block.PreviousBlockHash;
        }

        // if block state sets don't contain entity, return LIB value
        // lib value's block height should less than min block state set's block height.
        return libValue != null && libValue.Metadata.Block.BlockHeight <= currentBlockHeight && !libValue.IsDeleted ? libValue : null;
    }
    
    private async Task OperationAsync(TEntity entity, Func<string, TEntity,Task> confirmBlockFunc,
        Func<string, AppBlockStateSet, string, TEntity,Task> unConfirmBlockFunc)
    {
        if (!IsValidate(entity)) throw new Exception($"Invalid entity: {entity.ToJsonString()}");
        var entityKey = $"{_entityName}-{entity.Id}";
        
        var blockStateSetsGrainKey = GetBlockStateSetKey();
        var dataKey = GetAppDataKey(entityKey);
        
        var dataValue = await _dAppDataProvider.GetLibValueAsync<TEntity>(dataKey);
        var blockStateSets = await _blockStateSetProvider.GetBlockStateSetsAsync(blockStateSetsGrainKey);
        var blockStateSet = blockStateSets[entity.Metadata.Block.BlockHash];
        // Entity is confirmed,save it to es search directly
        if (blockStateSet.Block.Confirmed)
        {
            if ((dataValue?.Metadata.Block.BlockHeight??0) > blockStateSet.Block.BlockHeight)
            {
                return;
            }

            await confirmBlockFunc(dataKey, entity);
            return;
        }
        //Deal with fork
        var longestChainHashes = await _blockStateSetProvider.GetLongestChainHashesAsync(blockStateSetsGrainKey);
        // entity is on best chain
        if (longestChainHashes.ContainsKey(entity.Metadata.Block.BlockHash))
        {
            await unConfirmBlockFunc(blockStateSetsGrainKey, blockStateSet, entityKey, entity);
        }
        else // entity is not on best chain.
        {
            //if current block state is not on best chain, get the best chain block state set
            var longestChainBlockStateSet = await _blockStateSetProvider.GetLongestChainBlockStateSetAsync(blockStateSetsGrainKey);
            var entityFromBlockStateSet = GetEntityFromBlockStateSets(entityKey, blockStateSets, longestChainBlockStateSet.Block.BlockHash,
                longestChainBlockStateSet.Block.BlockHeight, dataValue);
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
    
    private bool IsValidate(TEntity entity)
    {
        return !string.IsNullOrWhiteSpace(entity.Metadata.Block.BlockHash) && entity.Metadata.Block.BlockHeight != 0 && entity.Id != null &&
               !string.IsNullOrWhiteSpace(entity.Metadata.Block.ChainId) && !string.IsNullOrWhiteSpace(entity.Metadata.Block.PreviousBlockHash);
    }

    private async Task AddToBlockStateSetAsync(string blockStateSetKey, AppBlockStateSet blockStateSet, string entityKey, TEntity entity)
    {
        entity.IsDeleted = false;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        await _dAppDataIndexProvider.AddOrUpdateAsync(entity, GetIndexName());
        await _blockStateSetProvider.SetBlockStateSetAsync(blockStateSetKey,blockStateSet);
    }
    
    private async Task RemoveFromBlockStateSetAsync(string blockStateSetKey, AppBlockStateSet blockStateSet, string entityKey,TEntity entity)
    {
        entity.IsDeleted = true;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        await _dAppDataIndexProvider.DeleteAsync(entity, GetIndexName());
        await _blockStateSetProvider.SetBlockStateSetAsync(blockStateSetKey, blockStateSet);
    }
    
    private string GetIndexName()
    {
        return $"{_blockProcessingContext.ScanAppId}-{_blockProcessingContext.Version}.{_entityName}".ToLower();
    }
    
    private string GetAppDataKey(string entityKey)
    {
        return GrainIdHelper.GenerateAppDataGrainId(_blockProcessingContext.ScanAppId, _blockProcessingContext.Version,
            _blockProcessingContext.ChainId, entityKey);
    }
    
    private string GetBlockStateSetKey()
    {
        return GrainIdHelper.GenerateAppBlockStateSetGrainId(_blockProcessingContext.ScanAppId, _blockProcessingContext.Version,
            _blockProcessingContext.ChainId);
    }
}