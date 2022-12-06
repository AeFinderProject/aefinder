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
                IsConfirmed = block.IsConfirmed,
                SignerPubkey = block.SignerPubkey,
                PreviousBlockHash = block.PreviousBlockHash
            })
            .ToList();
    }

    public async Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks, List<FilterContractEventInput> filters)
    {
        return blocks;
    }
}