using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockFilterProvider : IBlockFilterProvider
{
    private readonly IBlockAppService _blockAppService;

    public BlockFilterType FilterType { get; } = BlockFilterType.Block;

    public BlockFilterProvider(IBlockAppService blockAppService)
    {
        _blockAppService = blockAppService;
    }

    public async Task<List<BlockWithTransactionDto>> GetBlocksAsync(string chainId, long startBlockNumber, long endBlockNumber,
        bool onlyConfirmed, List<FilterContractEventInput> filters)
    {
        var blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
        {
            ChainId = chainId,
            HasTransaction = true,
            StartBlockHeight = startBlockNumber,
            EndBlockHeight = endBlockNumber,
            IsOnlyConfirmed = onlyConfirmed
        });

        return blocks.Select(block => new BlockWithTransactionDto
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
            //TODO previousBlockHash is null.
            if (block.PreviousBlockHash != previousBlockHash && previousBlockHash!=null || block.BlockHeight != previousBlockHeight + 1)
            {
                break;
            }
            
            filteredBlocks.Add(block);
            
            previousBlockHash = block.BlockHash;
            previousBlockHeight = block.BlockHeight;
        }

        return filteredBlocks;
    }
}