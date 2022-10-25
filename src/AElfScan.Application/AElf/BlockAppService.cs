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

    public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<Block>, QueryContainer>>();
        // mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).GreaterThanOrEquals(input.StartBlockNumber)));
        // mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).LessThanOrEquals(input.EndBlockNumber)));

        // mustQuery.Add(q=>q.Nested(n=>n.Path("Transaction")
            // .Query(sub=>sub.Bool(b=>b.Must(s=>s.Match(m=>m.Field("Transactions.LogEvents.eventName").Query("IrreversibleBlockFound")))))));
        
        if (input.Contracts!=null)
        {
            
            // mustQuery.Add(q => q.Nested(n =>
            //     n.Path(p=>p.Transactions).Query(sub =>
            //         sub.Bool(b=>b.Should(s=>s.Match(m=>m.Field("transactions.methodName").Query("DonateResourceToken")))))
            //     ));
            
            // mustQuery.Add(q => q.Nested(n =>
            //     n.Path(p=>p.Transactions).Query(sub =>
            //         sub.Bool(b=>b.Must(s=>s.Terms(m=>m.Field(f=>f.Transactions.FirstOrDefault().MethodName).Terms("DonateResourceToken")))))
            // ));
            
            mustQuery.Add(q=>q.Nested(n=>n.Path("Transaction")
                .Query(sub=>sub.Bool(b=>b.Must(s=>s.Match(m=>m.Field("Transactions.LogEvents.eventName").Query("IrreversibleBlockFound")))))));
            
            // mustQuery.Add(q=>q.Nested(n=>n.Path(p=>p.Transactions).Query(qq=>qq.Term(i=>i.Field(f=>f.Transactions.FirstOrDefault().MethodName).Value("DonateResourceToken")))));
            
            // mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.Transactions.FirstOrDefault().LogEvents.FirstOrDefault().ContractAddress).Value(input.ContractAddress)));
            // mustQuery.Add(q=>q.Term(i=>i.Field("transactions.logEvents.contractAddress").Value(input.ContractAddress)));
            
            // foreach (var eventName in input.EventNames)
            // {
            //     mustQuery.Add(q => q.Nested(n =>
            //         {
            //             n.Path("transactions");
            //             n.Query(q =>
            //                 q.Term(i => i.Field("transactions.logEvents.eventName")
            //                     .Value(input.ContractAddress)));
            //             return n;
            //         })
            //     );
            // }
        }
        mustQuery.Add(q => q.Range(i => i.Field("Transactions.blockNumber").GreaterThanOrEquals(input.StartBlockNumber)));
        mustQuery.Add(q => q.Range(i => i.Field("Transactions.blockNumber").LessThanOrEquals(input.EndBlockNumber)));
        mustQuery.Add(s =>
            s.Match(i=>i.Field("Transactions.LogEvents.eventName").Query("IrreversibleBlockFound")));
        
        QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Nested(q => q.Path("Transactions")
            .Query(qq => qq.Bool(b => b.Must(mustQuery))));

        // QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Nested(q => q.Path("Transactions")
        //     .Query(qq => qq.Bool(b => b.Must(s =>
        //         s.Match(i=>i.Field("Transactions.LogEvents.eventName").Query("IrreversibleBlockFound"))))));
        // QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Bool(b => b.Must(mustQuery));
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