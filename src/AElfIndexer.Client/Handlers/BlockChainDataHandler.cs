using System.Text;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class BlockChainDataHandler<TData> : IBlockChainDataHandler, ITransientDependency 
    where TData : BlockChainDataBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IDAppDataProvider _dAppDataProvider;
    private readonly IBlockStateSetProvider<TData> _blockStateSetProvider;
    private readonly IDAppDataIndexManagerProvider _dAppDataIndexManagerProvider;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    protected readonly IObjectMapper ObjectMapper;
    
    private string _version;
    private string _clientId;
    protected readonly ILogger<BlockChainDataHandler<TData>> Logger;
    
    protected BlockChainDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, ILogger<BlockChainDataHandler<TData>> logger,
        IDAppDataProvider dAppDataProvider, IBlockStateSetProvider<TData> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider)
    {
        _clusterClient = clusterClient;
        ObjectMapper = objectMapper;
        Logger = logger;
        _dAppDataProvider = dAppDataProvider;
        _blockStateSetProvider = blockStateSetProvider;
        _dAppDataIndexManagerProvider = dAppDataIndexManagerProvider;
        _clientInfoProvider = aelfIndexerClientInfoProvider;
    }

    public abstract BlockFilterType FilterType { get; }
    
    public async Task HandleBlockChainDataAsync(string chainId, string clientId, List<BlockWithTransactionDto> blockDtos)
    {
        _version = _clientInfoProvider.GetVersion();
        _clientId = _clientInfoProvider.GetClientId();
        
        var stateSetKey = GetBlockStateSetKey(chainId);
        var libHeight = 0L;
        var libHash = string.Empty;
        
        var longestChainBlockStateSet = await _blockStateSetProvider.GetLongestChainBlockStateSetAsync(stateSetKey);
        if (longestChainBlockStateSet != null && !longestChainBlockStateSet.Processed)
        {
            Logger.LogDebug("Handle unfinished longest chain data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}", chainId, _clientId, _version);
            await ProcessUnfinishedLongestChainAsync(stateSetKey, longestChainBlockStateSet);
        }

        var blockStateSets = await _blockStateSetProvider.GetBlockStateSetsAsync(stateSetKey);
        var longestChain = new List<BlockStateSet<TData>>();
        
        var libBlockHeight = blockStateSets.Count != 0 ? blockStateSets.Min(b => b.Value.BlockHeight) : 0;
        var longestHeight = blockStateSets.Count != 0 ? blockStateSets.Max(b => b.Value.BlockHeight) : 0;
        Logger.LogDebug(
            "Handle block data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}, Lib Height: {Lib}, Longest Chain Height: {LongestHeight}",
            chainId, _clientId, _version, libBlockHeight, longestHeight);
        foreach (var blockDto in blockDtos)
        {
            // Skip if block height less than lib block height
            if(blockDto.BlockHeight <= libBlockHeight)
            {
                continue;
            }

            if (blockStateSets.Count != 0 && !blockStateSets.ContainsKey(blockDto.PreviousBlockHash))
            {
                Logger.LogWarning(
                    $"Previous block {blockDto.PreviousBlockHash} not found. blockHeight: {blockDto.BlockHeight}, blockStateSets max block height: {blockStateSets.Max(b => b.Value.BlockHeight)}");
                continue;
            }

            if (blockStateSets.TryGetValue(blockDto.BlockHash, out var blockStateSet) && blockStateSet.Processed &&
                blockDto.Confirmed)
            {
                await DealWithConfirmBlockAsync(blockDto, blockStateSet);
                libHeight = blockDto.BlockHeight;
                libHash = blockDto.BlockHash;
                continue;
            }

            // Skip if blockStateSets contain block and processed
            if (blockStateSets.TryGetValue(blockDto.BlockHash, out blockStateSet) && blockStateSet.Processed)
            {
                continue;
            }

            if (blockStateSet == null)
            {
                blockStateSet = new BlockStateSet<TData>
                {
                    BlockHash = blockDto.BlockHash,
                    BlockHeight = blockDto.BlockHeight,
                    PreviousBlockHash = blockDto.PreviousBlockHash,
                    Confirmed = blockDto.Confirmed,
                    Data = GetData(blockDto),
                    Changes = new ()
                };
                await _blockStateSetProvider.SetBlockStateSetAsync(stateSetKey, blockStateSet);
                blockStateSets = await _blockStateSetProvider.GetBlockStateSetsAsync(stateSetKey);
            }
            if (longestChainBlockStateSet == null || blockDto.PreviousBlockHash == longestChainBlockStateSet.BlockHash)
            {
                longestChain.Add(blockStateSet);
                longestChainBlockStateSet = blockStateSet;
                await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, blockStateSet.BlockHash);
            }
            else if (blockDto.BlockHeight > longestChainBlockStateSet.BlockHeight)
            {
                var targetLongestChain = GetLongestChain(blockStateSets, blockDto.BlockHash);
                if (targetLongestChain.Count > 0)
                {
                    longestChain = targetLongestChain;
                    longestChainBlockStateSet = blockStateSet;
                    await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, blockStateSet.BlockHash);
                }
                else
                {
                    Logger.LogWarning("Longest chain not found.");
                }
            }
        }

        Logger.LogDebug("Handle longest chain data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}", chainId, _clientId, _version);
        if (longestChain.Count > 0)
        {
            await _blockStateSetProvider.SaveDataAsync(stateSetKey);
            await ProcessLongestChainAsync(stateSetKey, longestChain, blockStateSets);
            
            var confirmBlock = longestChain.LastOrDefault(b => b.Confirmed);
            if (confirmBlock != null && confirmBlock.BlockHeight > libHeight)
            {
                libHeight = confirmBlock.BlockHeight;
                libHash = confirmBlock.BlockHash;
            }
        }
        
        //Clean block state sets under latest lib block
        Logger.LogDebug("Handle lib data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}", chainId, _clientId, _version);
        if (libHeight != 0)
        {
            var blockStateSetInfoGrain =
                _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
                    GrainIdHelper.GenerateGrainId("BlockStateSetInfo", _clientId, chainId, _version));
            await blockStateSetInfoGrain.SetConfirmedBlockHeight(FilterType, libHeight);
            await _blockStateSetProvider.CleanBlockStateSetsAsync(stateSetKey, libHeight, libHash);
        }
        
        await _blockStateSetProvider.SaveDataAsync(stateSetKey);
    }

    private async Task ProcessUnfinishedLongestChainAsync(string blockStateSetKey, BlockStateSet<TData> longestChainBlockStateSet)
    {
        var blockStateSets = await _blockStateSetProvider.GetBlockStateSetsAsync(blockStateSetKey);
        var longestChain = GetLongestChain(blockStateSets, longestChainBlockStateSet.BlockHash);
        if (longestChain.Count > 0)
        {
            await ProcessLongestChainAsync(blockStateSetKey, longestChain, blockStateSets);
        }
    }

    private async Task ProcessLongestChainAsync(string blockStateSetKey, List<BlockStateSet<TData>> longestChain, 
        Dictionary<string, BlockStateSet<TData>> blockStateSets)
    {
        var bestChainBlockStateSet = await _blockStateSetProvider.GetBestChainBlockStateSetAsync(blockStateSetKey);
        var forkBlockStateSets = GetForkBlockStateSets(blockStateSets, longestChain, bestChainBlockStateSet?.BlockHash);
        
        await _blockStateSetProvider.SetLongestChainHashesAsync(blockStateSetKey, longestChain.ToDictionary(b => b.BlockHash,
            b => b.PreviousBlockHash));
        foreach (var blockStateSet in forkBlockStateSets)
        {
            //Set Current block state
            await _blockStateSetProvider.SetCurrentBlockStateSetAsync(blockStateSetKey, blockStateSet);
            await ProcessDataAsync(blockStateSet.Data);
            await _blockStateSetProvider.SetBlockStateSetProcessedAsync(blockStateSetKey, blockStateSet.BlockHash, false);
        }
        
        foreach (var blockStateSet in longestChain)
        {
            //Set Current block state
            await _blockStateSetProvider.SetCurrentBlockStateSetAsync(blockStateSetKey, blockStateSet);
            await ProcessDataAsync(blockStateSet.Data);
            await _blockStateSetProvider.SetBlockStateSetProcessedAsync(blockStateSetKey, blockStateSet.BlockHash, true);
        }

        var longestChainBlockStateSet = longestChain.LastOrDefault();
        if (longestChainBlockStateSet != null)
        {
            //Set BestChain
            await _blockStateSetProvider.SetBestChainBlockStateSetAsync(blockStateSetKey, longestChainBlockStateSet.BlockHash);
        }

        await _dAppDataProvider.SaveDataAsync();
        await _dAppDataIndexManagerProvider.SavaDataAsync();
    }

    private async Task DealWithConfirmBlockAsync(BlockWithTransactionDto blockDto, BlockStateSet<TData> blockStateSet)
    {
        //Deal with confirmed block
        foreach (var change in blockStateSet.Changes)
        {
            var dataKey = GrainIdHelper.GenerateGrainId("DAppData", _clientId, blockDto.ChainId, _version, change.Key);
            var value = await _dAppDataProvider.GetLibValueAsync<AElfIndexerClientEntity<string>>(dataKey);
            if (value != null && value.BlockHeight > blockDto.BlockHeight)
            {
                continue;
            }

            await _dAppDataProvider.SetLibValueAsync(dataKey, change.Value);
        }
    }

    private List<BlockStateSet<TData>> GetLongestChain(Dictionary<string,BlockStateSet<TData>> blockStateSets, string blockHash)
    {
        var longestChain = new List<BlockStateSet<TData>>();

        BlockStateSet<TData> blockStateSet;
        while (blockStateSets.TryGetValue(blockHash,out blockStateSet) && !blockStateSet.Processed)
        {
            longestChain.Add(blockStateSet);
            blockHash = blockStateSet.PreviousBlockHash;
        }

        if (blockStateSet == null && blockHash != "0000000000000000000000000000000000000000000000000000000000000000")
        {
            Logger.LogWarning($"Invalid block hash:{blockHash}");
            return new List<BlockStateSet<TData>>();
        }

        return longestChain.OrderBy(b => b.BlockHeight).ToList();
    }

    private List<BlockStateSet<TData>> GetForkBlockStateSets(Dictionary<string, BlockStateSet<TData>> blockStateSets,
        List<BlockStateSet<TData>> longestChain, string blockHash)
    {
        var forkBlockSateSets = new List<BlockStateSet<TData>>();
        if (blockHash == null || longestChain.Count == 0)
        {
            return forkBlockSateSets;
        }

        var longestChainPreviousBlockHashes = new HashSet<string>();
        foreach (var l in longestChain)
        {
            longestChainPreviousBlockHashes.Add(l.PreviousBlockHash);
        }
        while (!longestChainPreviousBlockHashes.Contains(blockHash) &&
               blockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            forkBlockSateSets.Add(blockStateSet);
            blockHash = blockStateSet.PreviousBlockHash;
        }
        return forkBlockSateSets.OrderBy(b => b.BlockHeight).ToList();
    }
    
    private string GetBlockStateSetKey(string chainId)
    {
        return GrainIdHelper.GenerateGrainId("BlockStateSets", _clientId, chainId, _version);
    }

    protected abstract List<TData> GetData(BlockWithTransactionDto blockDto);

    protected abstract Task ProcessDataAsync(List<TData> data);
}