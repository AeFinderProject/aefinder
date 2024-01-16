using AeFinder.Block;
using AeFinder.Block.Dtos;

namespace AeFinder.Grains.Grain.BlockScan;

public class BlockFilterProviderBase
{
    protected readonly IBlockAppService BlockAppService;

    public BlockFilterProviderBase(IBlockAppService blockAppService)
    {
        BlockAppService = blockAppService;
    }

    protected async Task<List<BlockWithTransactionDto>> FillVacantBlockAsync(string chainId, List<BlockWithTransactionDto> blocks, long startHeight, long endHeight, bool confirmed)
    {
        if (confirmed && blocks.Count == endHeight- startHeight + 1)
        {
            return blocks;
        }

        var result = new List<BlockWithTransactionDto>();
        var allBlocks = await BlockAppService.GetBlocksAsync(new GetBlocksInput
        {
            ChainId = chainId,
            StartBlockHeight = startHeight,
            EndBlockHeight = endHeight,
            IsOnlyConfirmed = confirmed
        });
        
        var filteredBlockDic = blocks.ToDictionary(o => o.BlockHash, o => o);
        foreach (var b in allBlocks)
        {
            if (filteredBlockDic.TryGetValue(b.BlockHash, out var filteredBlock))
            {
                result.Add(filteredBlock);
            }
            else
            {
                result.Add(new BlockWithTransactionDto
                {
                    Id = b.Id,
                    Signature = b.Signature,
                    BlockHash = b.BlockHash,
                    BlockHeight = b.BlockHeight,
                    BlockTime = b.BlockTime,
                    ChainId = b.ChainId,
                    ExtraProperties = b.ExtraProperties,
                    Confirmed = b.Confirmed,
                    SignerPubkey = b.SignerPubkey,
                    PreviousBlockHash = b.PreviousBlockHash
                });
            }
        }

        return result;
    }
}