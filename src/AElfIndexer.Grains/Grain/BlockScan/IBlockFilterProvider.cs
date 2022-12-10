using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IBlockFilterProvider
{
    BlockFilterType FilterType { get; }

    Task<List<BlockWithTransactionDto>> GetBlocksAsync(string chainId, long startBlockNumber, long endBlockNumber, bool onlyConfirmed,
        List<FilterContractEventInput> filters);
    
    Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks, List<FilterContractEventInput> filters);

    Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks);

    Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks, string previousBlockHash, long previousBlockHeight);
}