using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.EntityEventHandler.Core.Tests;

public class MockBlockAppService : IBlockAppService, ISingletonDependency
{
    public Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        throw new NotImplementedException();
    }

    public async Task<long> GetBlockCountAsync(GetBlocksInput input)
    {
        if (input.StartBlockHeight == 101)
        {
            return 10;
        }

        return 5;
    }

    public Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        throw new NotImplementedException();
    }

    public Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        throw new NotImplementedException();
    }
}