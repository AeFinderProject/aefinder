using AeFinder.Block.Dtos;

namespace AeFinder.Grains.Grain.BlockScan;

public interface IBlockFilterProvider
{
    BlockFilterType FilterType { get; }

    Task<List<BlockWithTransactionDto>> GetBlocksAsync(string chainId, long startBlockHeight, long endBlockHeight, bool onlyConfirmed,
        List<FilterContractEventInput> filters);
    
    Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks, List<FilterContractEventInput> filters);

    Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks);

    Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks, string previousBlockHash, long previousBlockHeight);
}