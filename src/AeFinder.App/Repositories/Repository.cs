using AeFinder.App.BlockProcessing;
using AeFinder.App.BlockState;
using AeFinder.App.OperationLimits;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.State.BlockStates;
using AeFinder.Sdk;
using AeFinder.Sdk.Entities;
using Newtonsoft.Json;

namespace AeFinder.App.Repositories;

public class Repository<TEntity> : RepositoryBase<TEntity>, IRepository<TEntity>
    where TEntity : AeFinderEntity, IAeFinderEntity
{
    private readonly IAppStateProvider _appStateProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppDataIndexProvider<TEntity> _appDataIndexProvider;
    private readonly IEntityOperationLimitProvider _entityOperationLimitProvider;
    private readonly IBlockProcessingContext _blockProcessingContext;

    private readonly Type _entityType;

    public Repository(IAppStateProvider appStateProvider, IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppDataIndexProvider<TEntity> appDataIndexProvider,
        IEntityOperationLimitProvider entityOperationLimitProvider, IBlockProcessingContext blockProcessingContext)
    {
        _appStateProvider = appStateProvider;
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appDataIndexProvider = appDataIndexProvider;
        _entityOperationLimitProvider = entityOperationLimitProvider;
        _blockProcessingContext = blockProcessingContext;
        _entityType = typeof(TEntity);
    }

    public async Task<TEntity> GetAsync(string id)
    {
        var chainId = _blockProcessingContext.ChainId;
        var blockIndex = new BlockIndex(_blockProcessingContext.BlockHash, _blockProcessingContext.BlockHeight);
        return await _appStateProvider.GetStateAsync<TEntity>(chainId, GetStateKey(id), blockIndex);
    }

    public async Task AddOrUpdateAsync(TEntity entity)
    {
        _entityOperationLimitProvider.Check(entity);
        SetMetadata(entity, false);
        CheckEntity(entity);
        var stateKey = GetStateKey(entity.Id);
        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(entity.Metadata.ChainId,entity.Metadata.Block.BlockHash);
        blockStateSet.Changes[stateKey] = GenerateAppState(entity);
        await _appDataIndexProvider.AddOrUpdateAsync(entity, GetIndexName());
        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(entity.Metadata.ChainId, blockStateSet);
    }
    
    public async Task DeleteAsync(string id)
    {
        var entity = await GetAsync(id);
        if (entity != null)
        {
            await DeleteAsync(entity);
        }
    }
    
    public async Task DeleteAsync(TEntity entity)
    {
        _entityOperationLimitProvider.Check(entity);
        SetMetadata(entity, true);
        CheckEntity(entity);
        var stateKey = GetStateKey(entity.Id);
        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(entity.Metadata.ChainId,entity.Metadata.Block.BlockHash);
        blockStateSet.Changes[stateKey] = GenerateAppState(entity);
        await _appDataIndexProvider.DeleteAsync(entity, GetIndexName());
        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(entity.Metadata.ChainId, blockStateSet);
    }

    private void CheckEntity(TEntity entity)
    {
        var checkResult = !string.IsNullOrWhiteSpace(entity.Metadata.Block.BlockHash) && entity.Metadata.Block.BlockHeight != 0 &&
               entity.Id != null &&
               !string.IsNullOrWhiteSpace(entity.Metadata.ChainId);
        if (!checkResult)
        {
            throw new Exception(
                $"Invalid entity: ChainId: {entity.Metadata.ChainId}, Id: {entity.Id}, BlockHash: {entity.Metadata.Block.BlockHash}, BlockHeight: {entity.Metadata.Block.BlockHeight}");
        }
    }

    private AppState GenerateAppState(TEntity entity)
    {
        return new AppState
        {
            Type = $"{_entityType.FullName},{_entityType.Assembly.FullName}",
            Value = JsonConvert.SerializeObject(entity)
        };
    }

    private string GetStateKey(string id)
    {
        return $"{_entityType.Name}-{id}";
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