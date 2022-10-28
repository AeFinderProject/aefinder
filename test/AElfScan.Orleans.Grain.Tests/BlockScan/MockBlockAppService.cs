using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScan.AElf;
using AElfScan.AElf.Dtos;
using Volo.Abp.DependencyInjection;

namespace AElfScan.BlockScan;

public class MockBlockAppService : IBlockAppService, ITransientDependency
{
    public async Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        var result = new List<BlockDto>();

        var previousHash = Guid.NewGuid().ToString();
        for (long i = input.StartBlockNumber; i <= input.EndBlockNumber; i++)
        {
            var blockHash = Guid.NewGuid().ToString();
            result.Add(new BlockDto
            {
                BlockNumber = i,
                BlockHash = blockHash,
                PreviousBlockHash = previousHash
            });

            previousHash = blockHash;
        }

        return result;
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