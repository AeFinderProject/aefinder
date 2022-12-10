using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class BlockChainDataHandler<TData,T> : IBlockChainDataHandler<T>, ITransientDependency 
    where TData : BlockChainDataBase
{
    private readonly IClusterClient _clusterClient;
    protected readonly IObjectMapper ObjectMapper;
    private readonly string _version;

    protected BlockChainDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper, IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider)
    {
        _clusterClient = clusterClient;
        ObjectMapper = objectMapper;
        _version = aelfIndexerClientInfoProvider.GetVersion();
    }
    
    public abstract BlockFilterType FilterType { get; }

    public async Task HandleBlockChainDataAsync(string chainId, string clientId, List<BlockDto> blockDtos)
    {
        var blockStateSetsGrain =
            _clusterClient.GetGrain<IBlockStateSetsGrain<TData>>(
                $"BlockStateSets_{clientId}_{chainId}_{_version}");
        var blockStateSets = await blockStateSetsGrain.GetBlockStateSets();
        var libBlockHeight = blockStateSets.Count != 0 ? blockStateSets.Min(b => b.Value.BlockHeight) : 0;
        if (!CheckLinked(blockDtos, blockStateSets)) return;
        if (!GetBlockMap(blockDtos, out var blockMap, out var bestChainBlockHashMap)) return;
        // Set best chain hashes
        await blockStateSetsGrain.SetBestChainHashes(bestChainBlockHashMap);
        // Order block by block height ascending.If there are many same height blocks, best chain block will be in first index.
        var orderBlocks = blockMap.OrderBy(b => b.Key);
        foreach (var (blockHeight, blocks) in orderBlocks)
        {
            // Skip if block height less than lib block height
            if(blockHeight <= libBlockHeight) continue;
            foreach (var block in blocks)
            {
                //TODO 重复区块不处理是否有问题
                //TODO 出现异常导致中断是否有影响
                // Skip if blockStateSets contain unconfirmed block 
                if (blockStateSets.TryGetValue(block.BlockHash, out var blockStateSet) && !block.Confirmed) continue;
                
                if (blockStateSet == null)
                {
                    blockStateSet = new BlockStateSet<TData>
                    {
                        BlockHash = block.BlockHash,
                        BlockHeight = block.BlockHeight,
                        PreviousBlockHash = block.PreviousBlockHash,
                        Changes = new Dictionary<string, string>(),
                        Data = GetData(block)
                    };
                }
                await blockStateSetsGrain.TryAddBlockStateSet(blockStateSet);
                await ProcessDataAsync(blockStateSet.Data);
            }
            var forkBlockStateSets = blockStateSets.Values.Where(b =>
                b.BlockHeight == blockHeight && !blocks.Select(blk=>blk.BlockHash).Contains(b.BlockHash)).ToList();
            foreach (var forkBlockStateSet in forkBlockStateSets)
            {
                await blockStateSetsGrain.TryAddBlockStateSet(forkBlockStateSet);
                await ProcessDataAsync(forkBlockStateSet.Data);
            }
        }

        //Clean block state sets under latest lib block
        var confirmBlock = blockDtos.LastOrDefault(b => b.Confirmed);
        if (confirmBlock != null)
        {
            await blockStateSetsGrain.CleanBlockStateSets(confirmBlock.BlockHeight, confirmBlock.BlockHash);
        }
    }

    protected abstract List<TData> GetData(BlockDto blockDto);

    protected abstract Task ProcessDataAsync(List<TData> data);
    
    private bool GetBlockMap(List<BlockDto> blockDtos, out Dictionary<long, List<BlockDto>> blockMap, out Dictionary<string,string> bestChainBlockHashMap) 
    {
        // Confirmed blocks do not need to check fork block.
        if (blockDtos.First().Confirmed)
        {
            blockMap = blockDtos.ToDictionary(b => b.BlockHeight, b => new List<BlockDto>
            {
                b
            });
            bestChainBlockHashMap = blockDtos.ToDictionary(b => b.BlockHash, b => b.PreviousBlockHash);
            return true;
        }
        //TODO 验证连续性?
        var maxBlockHeight = blockDtos.Max(b => b.BlockHeight);
        var minBlockHeight = blockDtos.Min(b => b.BlockHeight);
        blockMap = new ();
        bestChainBlockHashMap = new ();
        var currentBlock = blockDtos.First(b => b.BlockHeight == maxBlockHeight);;
        while (currentBlock!=null)
        {
            if (!blockMap.TryGetValue(currentBlock.BlockHeight, out var list))
            {
                list = new List<BlockDto>();
            }
            
            //Add best chain block 
            list.Add(currentBlock);
            bestChainBlockHashMap[currentBlock.BlockHash] = currentBlock.PreviousBlockHash;
            //Add fork blocks
            list.AddRange(blockDtos.Where(b =>
                b.BlockHeight == currentBlock.BlockHeight && b.BlockHash != currentBlock.BlockHash));
            blockMap[currentBlock.BlockHeight] = list;
            
            var currentBlockHeight = currentBlock.BlockHeight;
            currentBlock = blockDtos.FirstOrDefault(b => b.BlockHash == currentBlock.PreviousBlockHash);
            // latest block height should be equal to  minBlockHeight, otherwise it will return false
            if (currentBlock == null && currentBlockHeight != minBlockHeight) return false;
        }
        return true;
    }

    //Check min height block linked to block state sets
    private bool CheckLinked(List<BlockDto> blockDtos, Dictionary<string,BlockStateSet<TData>> blockStateSets)
    {
        var minBlockHeight = blockDtos.Min(b => b.BlockHeight);
        var minBlocks = blockDtos.Where(b => b.BlockHeight == minBlockHeight).ToList();
        return minBlocks.All(b => blockStateSets.ContainsKey(b.PreviousBlockHash)) || blockStateSets.Count == 0;
    }
}