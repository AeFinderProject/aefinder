using AElfIndexer.App.BlockProcessing;
using AElfIndexer.App.BlockState;
using AElfIndexer.App.OperationLimits;
using AElfIndexer.Grains.Grain.BlockStates;
using AElfIndexer.Sdk;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AElfIndexer.App.Repositories;

public class EntityRepository<TEntity> : IEntityRepository<TEntity>
    where TEntity : IndexerEntity, IIndexerEntity
{
    private readonly IAppStateProvider _appStateProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppDataIndexProvider<TEntity> _appDataIndexProvider;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IEntityOperationLimitProvider _entityOperationLimitProvider;
    private readonly IBlockProcessingContext _blockProcessingContext;

    private readonly string _entityName;

    public EntityRepository(IAppStateProvider appStateProvider, IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppDataIndexProvider<TEntity> appDataIndexProvider, 
        IEntityOperationLimitProvider entityOperationLimitProvider, IBlockProcessingContext blockProcessingContext, IAppInfoProvider appInfoProvider)
    {
        _appStateProvider = appStateProvider;
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appDataIndexProvider = appDataIndexProvider;
        _entityOperationLimitProvider = entityOperationLimitProvider;
        _blockProcessingContext = blockProcessingContext;
        _appInfoProvider = appInfoProvider;
        _entityName = typeof(TEntity).Name;
    }

    public async Task<TEntity> GetAsync(string id)
    {
        var chainId = _blockProcessingContext.ChainId;
        var blockIndex = new BlockIndex(_blockProcessingContext.BlockHash, _blockProcessingContext.BlockHeight);
        return await GetEntityFromBlockStateSetsAsync(chainId, GetEntityKey(chainId,id), blockIndex);
    }

    public async Task AddOrUpdateAsync(TEntity entity)
    {
        _entityOperationLimitProvider.Check(entity);
        SetMetadata(entity, false);
        await OperationAsync(entity, AddToBlockStateSetAsync, _blockProcessingContext.IsRollback);
    }
    
    public async Task DeleteAsync(TEntity entity)
    {
        _entityOperationLimitProvider.Check(entity);
        SetMetadata(entity, true);
        await OperationAsync(entity, RemoveFromBlockStateSetAsync, _blockProcessingContext.IsRollback);
    }

    private async Task<TEntity> GetEntityFromBlockStateSetsAsync(string chainId, string entityKey,
        IBlockIndex branchBlockIndex)
    {
        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId,branchBlockIndex.BlockHash);
        while (blockStateSet != null)
        {
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
            {
                var entity = JsonConvert.DeserializeObject<TEntity>(value);
                return (entity?.Metadata.IsDeleted ?? true) ? null : entity;
            }

            blockStateSet =
                await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, blockStateSet.Block.PreviousBlockHash);
        }

        // if block state sets don't contain entity, return LIB value
        // lib value's block height should less than min block state set's block height.
        var lastIrreversibleState =
            await _appStateProvider.GetLastIrreversibleStateAsync<TEntity>(chainId, entityKey);
        return lastIrreversibleState != null &&
               lastIrreversibleState.Metadata.Block.BlockHeight <= branchBlockIndex.BlockHeight &&
               !lastIrreversibleState.Metadata.IsDeleted
            ? lastIrreversibleState
            : null;
    }

    private async Task OperationAsync(TEntity entity, Func<string, TEntity, BlockStateSet, Task> operationFunc, bool isRollback)
    {
        if (!IsValidate(entity))
        {
            throw new Exception($"Invalid entity: {JsonConvert.SerializeObject(entity)}");
        }

        var entityKey = GetEntityKey(entity.Metadata.ChainId, entity.Id);

        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(entity.Metadata.ChainId,entity.Metadata.Block.BlockHash);

        if (isRollback)
        {
            // entity is not on best chain.
            // if current block state is not on best chain, get the best chain block state set
            var longestChainBlockStateSet =
                await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(entity.Metadata.ChainId);
            var entityFromBlockStateSet = await GetEntityFromBlockStateSetsAsync(entity.Metadata.ChainId, entityKey,
                new BlockIndex(longestChainBlockStateSet.Block.BlockHash, longestChainBlockStateSet.Block.BlockHeight));
            if (entityFromBlockStateSet != null)
            {
                await _appDataIndexProvider.AddOrUpdateAsync(entityFromBlockStateSet, GetIndexName());
            }
            else
            {
                await _appDataIndexProvider.DeleteAsync(entity, GetIndexName());
            }
        }
        else
        {
            // entity is on best chain
            await operationFunc(entityKey, entity, blockStateSet);
        }
    }

    private bool IsValidate(TEntity entity)
    {
        return !string.IsNullOrWhiteSpace(entity.Metadata.Block.BlockHash) && entity.Metadata.Block.BlockHeight != 0 &&
               entity.Id != null &&
               !string.IsNullOrWhiteSpace(entity.Metadata.ChainId);
    }

    private async Task AddToBlockStateSetAsync(string entityKey, TEntity entity, BlockStateSet blockStateSet)
    {
        entity.Metadata.IsDeleted = false;
        blockStateSet.Changes[entityKey] = JsonConvert.SerializeObject(entity);
        await _appDataIndexProvider.AddOrUpdateAsync(entity, GetIndexName());
        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(entity.Metadata.ChainId, blockStateSet);
    }

    private async Task RemoveFromBlockStateSetAsync(string entityKey,TEntity entity, BlockStateSet blockStateSet)
    {
        entity.Metadata.IsDeleted = true;
        blockStateSet.Changes[entityKey] = JsonConvert.SerializeObject(entity);
        await _appDataIndexProvider.DeleteAsync(entity, GetIndexName());
        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(entity.Metadata.ChainId, blockStateSet);
    }

    private string GetIndexName()
    {
        return $"{_appInfoProvider.AppId}-{_appInfoProvider.Version}.{_entityName}".ToLower();
    }
    
    private string GetEntityKey(string chainId, string id)
    {
        return $"{_entityName}-{chainId}-{id}";
    }
    
    private void SetMetadata(TEntity entity, bool isDelete)
    {
        entity.Metadata.ChainId = _blockProcessingContext.ChainId;
        entity.Metadata.Block = new BlockMetadata
        {
            BlockHash = _blockProcessingContext.BlockHash,
            BlockHeight = _blockProcessingContext.BlockHeight,
            BlockTime = _blockProcessingContext.BlockTime
        };
        entity.Metadata.IsDeleted = isDelete;
    }
}