using AeFinder.Block;
using AeFinder.Block.Dtos;
using Volo.Abp.DependencyInjection;

namespace AeFinder.EntityEventHandler;

public class MockBlockAppService : IBlockAppService, ISingletonDependency
{
    public Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        var blocks = new List<BlockDto>();
        for (long i = input.StartBlockHeight; i <= input.EndBlockHeight; i++)
        {
            blocks.Add(new BlockDto
            {
                BlockHash = "BlockHash" + i,
                PreviousBlockHash = "BlockHash" + (i - 1),
                BlockHeight = i,
            });
        }

        return Task.FromResult(blocks);
    }

    public Task<long> GetBlockCountAsync(GetBlocksInput input)
    {
        if (input.StartBlockHeight == 112)
        {
            return Task.FromResult<long>(5);
        }

        return Task.FromResult(input.EndBlockHeight - input.StartBlockHeight + 1);
    }

    public Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        throw new NotImplementedException();
    }

    public Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        throw new NotImplementedException();
    }

    public Task<List<TransactionDto>> GetSubscriptionTransactionsAsync(GetSubscriptionTransactionsInput input)
    {
        throw new NotImplementedException();
    }

    public Task<List<SummaryDto>> GetSummariesAsync(GetSummariesInput input)
    {
        throw new NotImplementedException();
    }
}