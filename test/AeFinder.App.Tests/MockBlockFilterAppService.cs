using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using Volo.Abp.ObjectMapping;

namespace AeFinder.App;

public class MockBlockFilterAppService : IBlockFilterAppService
{
    private readonly IObjectMapper _objectMapper;

    public MockBlockFilterAppService(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<List<BlockWithTransactionDto>> GetBlocksAsync(GetSubscriptionTransactionsInput input)
    {
        var blocks = BlockCreationHelper.CreateBlock(110, 10, "BlockHash", input.ChainId, "BlockHash109");
        return _objectMapper.Map<List<AppSubscribedBlockDto>, List<BlockWithTransactionDto>>(blocks);
    }

    public async Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks,
        List<FilterTransactionInput> transactionConditions = null,
        List<FilterContractEventInput> logEventConditions = null)
    {
        return blocks;
    }

    public async Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks)
    {
        return blocks;
    }

    public async Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks, string previousBlockHash,
        long previousBlockHeight)
    {
        return blocks;
    }
}