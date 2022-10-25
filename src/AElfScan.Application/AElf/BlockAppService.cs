using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using Nest;
using Volo.Abp.Application.Services;

namespace AElfScan.AElf;

public class BlockAppService:ApplicationService,IBlockAppService
{
    private readonly INESTRepository<Block, string> _liquidityRecordIndexRepository;
    
    public BlockAppService()
    {
        
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<Block>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).GreaterThanOrEquals(input.StartBlockNumber)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).LessThanOrEquals(input.EndBlockNumber)));

        if (!string.IsNullOrEmpty(input.ContractAddress))
        {
            mustQuery.Add(q => q.Nested(n =>
            {
                n.Path("transactions");
                n.Query(q =>
                    q.Term(i => i.Field("transactions.logEvents.contractAddress").Value(input.ContractAddress)));
                return n;
            }));
        }


        if (input.EventNames != null)
        {
            foreach (var eventName in input.EventNames)
            {
                mustQuery.Add(q => q.Nested(n =>
                    {
                        n.Path("transactions");
                        n.Query(q =>
                            q.Term(i => i.Field("transactions.logEvents.eventName")
                                .Value(input.ContractAddress)));
                        return n;
                    })
                );
            }
        }

        QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _liquidityRecordIndexRepository.GetListAsync(Filter);

        var items = ObjectMapper.Map<List<Block>, List<BlockDto>>(list.Item2);

        List<TransactionDto> resultList = new List<TransactionDto>();
        foreach (var blockItem in items)
        {
            resultList.AddRange(blockItem.Transactions);
        }

        return resultList;
    }

}