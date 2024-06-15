using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;

namespace AeFinder.BlockScan;

public interface IBlockFilterAppService
{
    Task<List<BlockWithTransactionDto>> GetBlocksAsync(GetSubscriptionTransactionsInput input);

    Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks,
        List<FilterTransactionInput> transactionConditions = null,
        List<FilterContractEventInput> logEventConditions = null);

    Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks);

    Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks, string previousBlockHash, long previousBlockHeight);
}