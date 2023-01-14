using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class BlockChainDataHandler<TData> : IBlockChainDataHandler, ITransientDependency 
    where TData : BlockChainDataBase
{
    private readonly IClusterClient _clusterClient;
    protected readonly IObjectMapper ObjectMapper;
    private readonly string _version;
    private readonly string _clientId;
    protected readonly ILogger<BlockChainDataHandler<TData>> Logger;
    
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    protected IDAppDataProvider DAppDataProvider => this.LazyServiceProvider.LazyGetRequiredService<IDAppDataProvider>();
    protected IBlockStateSetProvider<TData> BlockStateSetProvider => this.LazyServiceProvider.LazyGetRequiredService<IBlockStateSetProvider<TData>>();

    protected BlockChainDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper, IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, ILogger<BlockChainDataHandler<TData>> logger)
    {
        _clusterClient = clusterClient;
        ObjectMapper = objectMapper;
        Logger = logger;
        _version = aelfIndexerClientInfoProvider.GetVersion();
        _clientId = aelfIndexerClientInfoProvider.GetClientId();
    }
    
    public abstract BlockFilterType FilterType { get; }
    
    public async Task HandleBlockChainDataAsync(string chainId, string clientId, List<BlockWithTransactionDto> blockDtos)
    {
        var stateSetKey = GetBlockStateSetKey(chainId);
        
        var blockStateSets = await BlockStateSetProvider.GetBlockStateSets(stateSetKey);
        var longestChainBlockStateSet = await BlockStateSetProvider.GetLongestChainBlockStateSet(stateSetKey);

        var longestChain = new List<BlockStateSet<TData>>();
        if (longestChainBlockStateSet != null && !longestChainBlockStateSet.Processed)
        {
            longestChain = GetLongestChain(blockStateSets, longestChainBlockStateSet.BlockHash);
            if (longestChain.Count > 0)
            {
                await ProcessLongestChainAsync(stateSetKey, longestChain, blockStateSets);
                await DAppDataProvider.CommitAsync();
                blockStateSets = await BlockStateSetProvider.GetBlockStateSets(stateSetKey);
                longestChain = new List<BlockStateSet<TData>>();
            }
        }
        
        if (!CheckLinked(blockDtos, blockStateSets)) return;
        
        var libBlockHeight = blockStateSets.Count != 0 ? blockStateSets.Min(b => b.Value.BlockHeight) : 0;
        foreach (var blockDto in blockDtos)
        {
            // Skip if block height less than lib block height
            if(blockDto.BlockHeight <= libBlockHeight) continue;
            if (blockStateSets.TryGetValue(blockDto.BlockHash, out var blockStateSet) && blockStateSet.Processed &&
                blockDto.Confirmed)
            {
                await DealWithConfirmBlockAsync(blockDto, blockStateSet);
                continue;
            }

            // Skip if blockStateSets contain block and processed
            if (blockStateSets.TryGetValue(blockDto.BlockHash, out blockStateSet) && blockStateSet.Processed) continue;
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
                await BlockStateSetProvider.AddBlockStateSet(stateSetKey, blockStateSet);
            }
            if (longestChainBlockStateSet == null || blockDto.PreviousBlockHash == longestChainBlockStateSet.BlockHash)
            {
                longestChain.Add(blockStateSet);
                longestChainBlockStateSet = blockStateSet;
                await BlockStateSetProvider.SetLongestChainBlockStateSet(stateSetKey, blockStateSet.BlockHash);
            }
            else if (blockDto.BlockHeight > longestChainBlockStateSet.BlockHeight)
            {
                blockStateSets = await BlockStateSetProvider.GetBlockStateSets(stateSetKey);
                var targetLongestChain = GetLongestChain(blockStateSets, blockDto.BlockHash);
                if (targetLongestChain.Count > 0)
                {
                    longestChain = targetLongestChain;
                    longestChainBlockStateSet = blockStateSet;
                    await BlockStateSetProvider.SetLongestChainBlockStateSet(stateSetKey, blockStateSet.BlockHash);
                }
            }
        }

        if (longestChain.Count > 0)
        {
            await ProcessLongestChainAsync(stateSetKey, longestChain, blockStateSets);
        }

        await DAppDataProvider.CommitAsync();

        //Clean block state sets under latest lib block
        var confirmBlock = blockDtos.LastOrDefault(b => b.Confirmed);
        if (confirmBlock != null)
        {
            await BlockStateSetProvider.CleanBlockStateSets(stateSetKey, confirmBlock.BlockHeight, confirmBlock.BlockHash);
            var blockStateSetInfoGrain =
                _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
                    GrainIdHelper.GenerateGrainId("BlockStateSetInfo", _clientId, chainId, _version));
            await blockStateSetInfoGrain.SetConfirmedBlockHeight(FilterType, confirmBlock.BlockHeight);
        }
    }

    private async Task ProcessLongestChainAsync(string blockStateSetKey, List<BlockStateSet<TData>> longestChain, 
        Dictionary<string, BlockStateSet<TData>> blockStateSets)
    {
        var bestChainBlockStateSet = await BlockStateSetProvider.GetBestChainBlockStateSet(blockStateSetKey);
        var forkBlockStateSets = GetForkBlockStateSets(blockStateSets, longestChain, bestChainBlockStateSet?.BlockHash);
        
        await BlockStateSetProvider.SetLongestChainHashes(blockStateSetKey, longestChain.ToDictionary(b => b.BlockHash,
            b => b.PreviousBlockHash));
        foreach (var blockStateSet in forkBlockStateSets)
        {
            //Set Current block state
            await BlockStateSetProvider.SetCurrentBlockStateSet(blockStateSetKey, blockStateSet);
            await ProcessDataAsync(blockStateSet.Data);
        }
        
        foreach (var blockStateSet in longestChain)
        {
            //Set Current block state
            await BlockStateSetProvider.SetCurrentBlockStateSet(blockStateSetKey, blockStateSet);
            await ProcessDataAsync(blockStateSet.Data);
            await BlockStateSetProvider.SetBlockStateSetProcessed(blockStateSetKey, blockStateSet.BlockHash);
        }

        var longestChainBlockStateSet = longestChain.LastOrDefault();
        if (longestChainBlockStateSet != null)
        {
            //Set BestChain
            await BlockStateSetProvider.SetBestChainBlockStateSet(blockStateSetKey, longestChainBlockStateSet.BlockHash);
        }

        //await BlockStateSetProvider.CommitAsync(blockStateSetKey);
    }

    private async Task DealWithConfirmBlockAsync(BlockWithTransactionDto blockDto,BlockStateSet<TData> blockStateSet)
    {
        try
        {
            //Deal with confirmed block
            foreach (var change in blockStateSet.Changes)
            {
                var dappDataKey = GrainIdHelper.GenerateGrainId("DAppData", _clientId, blockDto.ChainId, _version, change.Key);
                var value = await DAppDataProvider.GetLibValueAsync<AElfIndexerClientEntity<string>>(dappDataKey);
                if (value != null && value.BlockHeight > blockDto.BlockHeight)
                {
                    continue;
                }

                await DAppDataProvider.SetLibValueAsync(dappDataKey, change.Value);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, e.Message);
            throw;
        }
        
    }
    private List<BlockStateSet<TData>> GetLongestChain(Dictionary<string,BlockStateSet<TData>> blockStateSets, string blockHash)
    {
        var longestChain = new List<BlockStateSet<TData>>();
        if (blockStateSets.Count == 0 || blockHash == null)
            return longestChain;
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
        if (blockHash == null || longestChain.Count == 0) return forkBlockSateSets;
        var longestChainStart = longestChain[0];
        var longestChainPreviousBlockHashes = new HashSet<string>();
        foreach (var l in longestChain)
        {
            longestChainPreviousBlockHashes.Add(l.PreviousBlockHash);
        }
        while (!longestChainPreviousBlockHashes.Contains(blockHash) &&
               blockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            if (blockStateSet.BlockHeight == longestChainStart.BlockHeight - 1)
            {
                longestChainStart = blockStateSets[longestChainStart.PreviousBlockHash];
                if (blockStateSet.BlockHash == longestChainStart.BlockHash) break;
            }

            forkBlockSateSets.Add(blockStateSet);
            blockHash = blockStateSet.PreviousBlockHash;
        }
        return forkBlockSateSets.OrderBy(b => b.BlockHeight).ToList();
    }

    protected abstract List<TData> GetData(BlockWithTransactionDto blockDto);

    protected abstract Task ProcessDataAsync(List<TData> data);
    

    //Check min height block linked to block state sets
    private bool CheckLinked(List<BlockWithTransactionDto> blockDtos, Dictionary<string,BlockStateSet<TData>> blockStateSets)
    {
        return blockDtos.Any(b => blockStateSets.ContainsKey(b.PreviousBlockHash)) || blockStateSets.Count == 0;
    }

    private string GetBlockStateSetKey(string chainId)
    {
        return GrainIdHelper.GenerateGrainId("BlockStateSets", _clientId, chainId, _version);
    }
}