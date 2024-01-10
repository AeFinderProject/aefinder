using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public interface IBlockFilterProvider
{
    Task<List<BlockWithTransactionDto>> GetBlocksAsync(GetSubscriptionTransactionsInput input);
    
    Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks, List<TransactionFilter> transactionFilters, List<LogEventFilter> logEventFilters);

    Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks);

    Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks, string previousBlockHash, long previousBlockHeight);
}