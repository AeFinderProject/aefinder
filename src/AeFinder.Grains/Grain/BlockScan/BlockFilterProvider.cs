using AeFinder.Block;
using AeFinder.Block.Dtos;
using Microsoft.Extensions.Logging;

namespace AeFinder.Grains.Grain.BlockScan;

public class BlockFilterProvider : BlockFilterProviderBase,IBlockFilterProvider
{
    private readonly ILogger<BlockFilterProvider> _logger;

    public BlockFilterType FilterType { get; } = BlockFilterType.Block;

    public BlockFilterProvider(IBlockAppService blockAppService, ILogger<BlockFilterProvider> logger)
        : base(blockAppService)
    {
        _logger = logger;
    }

    public async Task<List<BlockWithTransactionDto>> GetBlocksAsync(string chainId, long startBlockHeight, long endBlockHeight,
        bool onlyConfirmed, List<FilterContractEventInput> filters)
    {
        var blocks = await BlockAppService.GetBlocksAsync(new GetBlocksInput
        {
            ChainId = chainId,
            HasTransaction = true,
            StartBlockHeight = startBlockHeight,
            EndBlockHeight = endBlockHeight,
            IsOnlyConfirmed = onlyConfirmed
        });
        
        var result = blocks.Select(block => new BlockWithTransactionDto
            {
                Id = block.Id,
                Signature = block.Signature,
                BlockHash = block.BlockHash,
                BlockHeight = block.BlockHeight,
                BlockTime = block.BlockTime,
                ChainId = block.ChainId,
                ExtraProperties = block.ExtraProperties,
                Confirmed = block.Confirmed,
                SignerPubkey = block.SignerPubkey,
                PreviousBlockHash = block.PreviousBlockHash
            })
            .ToList();

        if (result.Count != 0 && result.First().BlockHeight != startBlockHeight)
        {
            throw new ApplicationException(
                $"Get Block filed, ChainId {chainId} StartBlockHeight {startBlockHeight} EndBlockHeight {endBlockHeight} OnlyConfirmed {onlyConfirmed}, Result first block height {result.First().BlockHeight}");
        }

        return result;
    }

    public async Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks, List<FilterContractEventInput> filters)
    {
        return blocks;
    }
    
    public async Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId, List<BlockWithTransactionDto> blocks)
    {
        return blocks;
    }

    public async Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId, 
        List<BlockWithTransactionDto> blocks, string previousBlockHash, long previousBlockHeight)
    {
        var filteredBlocks = new List<BlockWithTransactionDto>();
        foreach (var block in blocks)
        {
            if (block.PreviousBlockHash != previousBlockHash && previousBlockHash!=null || block.BlockHeight != previousBlockHeight + 1)
            {
                _logger.LogWarning($"Wrong confirmed previousBlockHash or previousBlockHash: block hash {block.BlockHash}, block height {block.BlockHeight}");
                break;
            }
            
            filteredBlocks.Add(block);
            
            previousBlockHash = block.BlockHash;
            previousBlockHeight = block.BlockHeight;
        }

        return filteredBlocks;
    }
}