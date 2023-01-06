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
        var blockStateSetsGrain =
            _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", clientId, chainId, _version));
        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        var longestChainBlockStateSet = await blockStateSetsGrain.GetLongestChainBlockStateSet();

        var longestChain = GetLongestChain(blockStateSets, longestChainBlockStateSet?.BlockHash);
        await blockStateSetsGrain.SetLongestChainHashes(longestChain.ToDictionary(b => b.BlockHash,
            b => b.PreviousBlockHash));
        await ProcessLongestChainAsync(longestChain, blockStateSetsGrain);
        if (longestChain.Count > 0)
        {
            blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
            longestChain = new List<BlockStateSet<TData>>();
        }
        
        if (!CheckLinked(blockDtos, blockStateSets)) return;
        
        var libBlockHeight = blockStateSets.Count != 0 ? blockStateSets.Min(b => b.Value.BlockHeight) : 0;
        var forkBlockStateSets = new List<BlockStateSet<TData>>();
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
                    Changes = new Dictionary<string, string>(),
                    Confirmed = blockDto.Confirmed,
                    Data = GetData(blockDto)
                };
            }
            await blockStateSetsGrain.TryAddBlockStateSet(blockStateSet);
            //TODO deal with process longestchain 中断
            // TODO 如果longestchain 和 bestchain不一样则有中断
            if (longestChainBlockStateSet == null || blockDto.PreviousBlockHash == longestChainBlockStateSet.BlockHash)
            {
                longestChain.Add(blockStateSet);
                longestChainBlockStateSet = blockStateSet;
                await blockStateSetsGrain.SetLongestChainBlockStateSet(blockStateSet.BlockHash);
            }
            else if (blockDto.BlockHeight > longestChainBlockStateSet.BlockHeight)
            {
                blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
                longestChain = GetLongestChain(blockStateSets, blockDto.BlockHash);
                
                if (longestChain.All(b => b.BlockHash != longestChainBlockStateSet.BlockHash))
                {
                    var bestChainBlockStateSet = await blockStateSetsGrain.GetBestChainBlockStateSet();
                    forkBlockStateSets = GetForkBlockStateSets(blockStateSets, longestChain, bestChainBlockStateSet.BlockHash);
                }
                longestChainBlockStateSet = blockStateSet;
                await blockStateSetsGrain.SetLongestChainBlockStateSet(blockStateSet.BlockHash);
            }
        }
        await blockStateSetsGrain.SetLongestChainHashes(longestChain.ToDictionary(b => b.BlockHash,
            b => b.PreviousBlockHash));
        
        foreach (var blockStateSet in forkBlockStateSets)
        {
            //Set Current block state
            await blockStateSetsGrain.SetCurrentBlockStateSet(blockStateSet);
            await ProcessDataAsync(blockStateSet.Data);
        }

        await ProcessLongestChainAsync(longestChain, blockStateSetsGrain);

        //Clean block state sets under latest lib block
        var confirmBlock = blockDtos.LastOrDefault(b => b.Confirmed);
        if (confirmBlock != null)
        {
            await blockStateSetsGrain.CleanBlockStateSets(confirmBlock.BlockHeight, confirmBlock.BlockHash);
            var blockStateSetInfoGrain =
                _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
                    GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, chainId, _version));
            await blockStateSetInfoGrain.SetConfirmedBlockHeight(FilterType, confirmBlock.BlockHeight);
        }
    }

    private async Task ProcessLongestChainAsync(List<BlockStateSet<TData>> longestChain, IBlockStateSetsGrain<TData> blockStateSetsGrain)
    {
        foreach (var blockStateSet in longestChain)
        {
            //Set Current block state
            await blockStateSetsGrain.SetCurrentBlockStateSet(blockStateSet);
            await ProcessDataAsync(blockStateSet.Data);
            await blockStateSetsGrain.SetBlockStateSetProcessed(blockStateSet.BlockHash);
        }

        var longestChainBlockStateSet = longestChain.LastOrDefault();
        if (longestChainBlockStateSet != null)
        {
            //Set BestChain
            await blockStateSetsGrain.SetBestChainBlockStateSet(longestChainBlockStateSet.BlockHash);
        }
    }
    
    
    
    private async Task DealWithConfirmBlockAsync(BlockWithTransactionDto blockDto,BlockStateSet<TData> blockStateSet)
    {
        try
        {
            var tasks = new List<Task>();
            //Deal with confirmed block
            foreach (var change in blockStateSet.Changes)
            {
                tasks.Add(Task.Factory.StartNew(async () =>
                {
                    var dappGrain = _clusterClient.GetGrain<IDappDataGrain>(
                        GrainIdHelper.GenerateGrainId("DappData", _clientId, blockDto.ChainId, _version, change.Key));
                    var value = await dappGrain.GetLIBValue<AElfIndexerClientEntity<string>>();
                    if (value != null && value.BlockHeight > blockDto.BlockHeight) return;
                    await dappGrain.SetLIBValue(change.Value);
                }).Unwrap());
            }
            
            await tasks.WhenAll();
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
        if (blockStateSets.Count == 0 || blockHash == null) return longestChain;
        BlockStateSet<TData> blockStateSet;
        while (blockStateSets.TryGetValue(blockHash,out blockStateSet) && !blockStateSet.Processed)
        {
            longestChain.Add(blockStateSet);
            blockHash = blockStateSet.PreviousBlockHash;
        }

        if (blockStateSet == null && blockHash != "0000000000000000000000000000000000000000000000000000000000000000") throw new Exception($"Invalid block hash:{blockHash}");

        return longestChain.OrderBy(b => b.BlockHeight).ToList();
    }

    private List<BlockStateSet<TData>> GetForkBlockStateSets(Dictionary<string, BlockStateSet<TData>> blockStateSets,
        List<BlockStateSet<TData>> longestChain, string blockHash)
    {
        var forkBlockSateSets = new List<BlockStateSet<TData>>();
        while (longestChain.All(b => b.PreviousBlockHash != blockHash) &&
               blockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
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
}