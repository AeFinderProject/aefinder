using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElfScan.AElf;

[RemoteService(IsEnabled = false)]
public class BlockAppService:ApplicationService,IBlockAppService
{
    private readonly INESTRepository<Block, string> _blockIndexRepository;
    
    public BlockAppService(INESTRepository<Block, string> blockIndexRepository)
    {
        _blockIndexRepository = blockIndexRepository;
    }

    public async Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<Block>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).GreaterThanOrEquals(input.StartBlockNumber)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).LessThanOrEquals(input.EndBlockNumber)));
        
        QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _blockIndexRepository.GetListAsync(Filter);

        var items = ObjectMapper.Map<List<Block>, List<BlockDto>>(list.Item2);
        
        return items;
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<Block>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i => i.Field("Transactions.blockNumber").GreaterThanOrEquals(input.StartBlockNumber)));
        mustQuery.Add(q => q.Range(i => i.Field("Transactions.blockNumber").LessThanOrEquals(input.EndBlockNumber)));
        
        if (input.Contracts!=null)
        {
            foreach (ContractInput contractInput in input.Contracts)
            {
                if (!string.IsNullOrEmpty(contractInput.ContractAddress))
                {
                    mustQuery.Add(s =>
                        s.Match(i=>i.Field("Transactions.LogEvents.contractAddress").Query(contractInput.ContractAddress)));
                }
                foreach (var eventName in contractInput.EventNames)
                {
                    mustQuery.Add(s =>
                        s.Match(i=>i.Field("Transactions.LogEvents.eventName").Query(eventName)));
                }
            }
            
        }
        
        QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Nested(q => q.Path("Transactions")
            .Query(qq => qq.Bool(b => b.Must(mustQuery))));

        // QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Nested(q => q.Path("Transactions")
        //     .Query(qq => qq.Bool(b => b.Must(s =>
        //         s.Match(i=>i.Field("Transactions.LogEvents.eventName").Query("IrreversibleBlockFound"))))));
        List<TransactionDto> resultList = new List<TransactionDto>();

        try
        {
            var list = await _blockIndexRepository.GetListAsync(Filter);

            var items = ObjectMapper.Map<List<Block>, List<BlockDto>>(list.Item2);

            foreach (var blockItem in items)
            {
                resultList.AddRange(blockItem.Transactions);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }

        return resultList;
    }

}