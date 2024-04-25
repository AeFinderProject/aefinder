using AeFinder.Block.Dtos;
using AeFinder.Grains.Grain.Subscriptions;

namespace AeFinder.Grains.Grain.BlockPush;

public interface IBlockFilterProvider
{
    Task<List<BlockWithTransactionDto>> GetBlocksAsync(GetSubscriptionTransactionsInput input);
    
    Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks, List<TransactionCondition> transactionConditions, List<LogEventCondition> logEventConditions);

    Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks);

    Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks, string previousBlockHash, long previousBlockHeight);
}