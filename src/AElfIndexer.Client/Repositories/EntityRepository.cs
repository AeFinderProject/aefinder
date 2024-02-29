using AElfIndexer.Client.BlockState;
using AElfIndexer.Client.OperationLimits;
using AElfIndexer.Grains.Grain.BlockStates;
using AElfIndexer.Sdk;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AElfIndexer.Client.Repositories;

public class EntityRepository<TEntity> : IEntityRepository<TEntity>
    where TEntity : IndexerEntity, IIndexerEntity
{
    private readonly IAppStateProvider _appStateProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppDataIndexProvider<TEntity> _appDataIndexProvider;
    private readonly AppInfoOptions _appInfoOptions;
    private readonly IEntityOperationLimitProvider _entityOperationLimitProvider;

    private readonly string _entityName;

    public EntityRepository(IAppStateProvider appStateProvider, IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppDataIndexProvider<TEntity> appDataIndexProvider, IOptionsSnapshot<AppInfoOptions> appInfoOptions,
        IEntityOperationLimitProvider entityOperationLimitProvider)
    {
        _appStateProvider = appStateProvider;
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appDataIndexProvider = appDataIndexProvider;
        _entityOperationLimitProvider = entityOperationLimitProvider;
        _appInfoOptions = appInfoOptions.Value;
        _entityName = typeof(TEntity).Name;
    }

    public async Task<TEntity> GetAsync(string chainId, IBlockIndex blockIndex, string id)
    {
        var entityKey = $"{_entityName}-{id}";
        return await GetEntityFromBlockStateSetsAsync(chainId, entityKey, blockIndex);
    }

    public async Task AddOrUpdateAsync(TEntity entity, bool isRollback)
    {
        _entityOperationLimitProvider.Check(entity);
        entity.Metadata.IsDeleted = false;
        await OperationAsync(entity, AddToBlockStateSetAsync, isRollback);
    }
    
    public async Task DeleteAsync(TEntity entity, bool isRollback)
    {
        _entityOperationLimitProvider.Check(entity);
        entity.Metadata.IsDeleted = true;
        await OperationAsync(entity, RemoveFromBlockStateSetAsync, isRollback);
    }

    private async Task<TEntity> GetEntityFromBlockStateSetsAsync(string chainId, string entityKey,
        IBlockIndex branchBlockIndex)
    {
        var blockStateSets = await _appBlockStateSetProvider.GetBlockStateSetsAsync(chainId);
        var blockHash = branchBlockIndex.BlockHash;
        while (blockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            if (blockStateSet.Changes.TryGetValue(entityKey, out var value))
            {
                var entity = JsonConvert.DeserializeObject<TEntity>(value);
                return (entity?.Metadata.IsDeleted ?? true) ? null : entity;
            }

            blockHash = blockStateSet.Block.PreviousBlockHash;
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
            throw new Exception($"Invalid entity: {entity.ToJsonString()}");
        }

        var entityKey = $"{_entityName}-{entity.Id}";

        var blockStateSets = await _appBlockStateSetProvider.GetBlockStateSetsAsync(entity.Metadata.ChainId);
        var blockStateSet = blockStateSets[entity.Metadata.Block.BlockHash];

        if (isRollback)
        {
            // entity is not on best chain.
            //if current block state is not on best chain, get the best chain block state set
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
               !string.IsNullOrWhiteSpace(entity.Metadata.ChainId) &&
               !string.IsNullOrWhiteSpace(entity.Metadata.Block.PreviousBlockHash);
    }

    private async Task AddToBlockStateSetAsync(string entityKey, TEntity entity, BlockStateSet blockStateSet)
    {
        entity.Metadata.IsDeleted = false;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        await _appDataIndexProvider.AddOrUpdateAsync(entity, GetIndexName());
        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(entity.Metadata.ChainId, blockStateSet);
    }

    private async Task RemoveFromBlockStateSetAsync(string entityKey,TEntity entity, BlockStateSet blockStateSet)
    {
        entity.Metadata.IsDeleted = true;
        blockStateSet.Changes[entityKey] = entity.ToJsonString();
        await _appDataIndexProvider.DeleteAsync(entity, GetIndexName());
        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(entity.Metadata.ChainId, blockStateSet);
    }

    private string GetIndexName()
    {
        return $"{_appInfoOptions.AppId}-{_appInfoOptions.Version}.{_entityName}".ToLower();
    }
}