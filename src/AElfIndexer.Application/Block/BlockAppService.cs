using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Entities.Es;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElfIndexer.Block;

[RemoteService(IsEnabled = false)]
public class BlockAppService:ApplicationService,IBlockAppService
{
    private readonly INESTRepository<BlockIndex, string> _blockIndexRepository;
    private readonly INESTRepository<TransactionIndex, string> _transactionIndexRepository;
    private readonly INESTRepository<LogEventIndex, string> _logEventIndexRepository;
    private readonly ApiOptions _apiOptions;
    
    public BlockAppService(INESTRepository<BlockIndex, string> blockIndexRepository,
        INESTRepository<TransactionIndex,string> transactionIndexRepository,
        INESTRepository<LogEventIndex,string> logEventIndexRepository,
        IOptionsSnapshot<ApiOptions> apiOptions)
    {
        _blockIndexRepository = blockIndexRepository;
        _transactionIndexRepository = transactionIndexRepository;
        _logEventIndexRepository = logEventIndexRepository;
        _apiOptions = apiOptions.Value;
    }

    public async Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        if ((input.EndBlockHeight - input.StartBlockHeight+1) > _apiOptions.BlockQueryHeightInterval)
        {
            input.EndBlockHeight = input.StartBlockHeight + _apiOptions.BlockQueryHeightInterval-1;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();

        List<BlockDto> items = new List<BlockDto>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        // mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId.Suffix("keyword")).Value(input.ChainId)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).GreaterThanOrEquals(input.StartBlockHeight)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(input.EndBlockHeight)));

        if (input.IsOnlyConfirmed)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsConfirmed).Value(input.IsOnlyConfirmed)));
        }

        QueryContainer Filter(QueryContainerDescriptor<BlockIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _blockIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockHeight,
            sortType: SortOrder.Ascending, limit: int.MaxValue);
        items = ObjectMapper.Map<List<BlockIndex>, List<BlockDto>>(list.Item2);

        List<BlockDto> resultList = new List<BlockDto>();
        if (!input.HasTransaction)
        {
            foreach (var blockItem in items)
            {
                resultList.Add(blockItem);
            }
        }
        else
        {
            resultList.AddRange(items);
        }

        return resultList;
    }
    
    public async Task<long> GetBlockCountAsync(GetBlocksInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).GreaterThanOrEquals(input.StartBlockHeight)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(input.EndBlockHeight)));

        if (input.IsOnlyConfirmed)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsConfirmed).Value(input.IsOnlyConfirmed)));
        }

        QueryContainer Filter(QueryContainerDescriptor<BlockIndex> f) => f.Bool(b => b.Must(mustQuery));

        var count = await _blockIndexRepository.CountAsync(Filter);
        return count.Count;
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        if (input.EndBlockHeight - input.StartBlockHeight > _apiOptions.TransactionQueryHeightInterval)
        {
            input.EndBlockHeight = input.StartBlockHeight + _apiOptions.TransactionQueryHeightInterval;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<TransactionIndex>, QueryContainer>>();
        
        List<TransactionDto> resultList = new List<TransactionDto>();
        if (input.Events != null && input.Events.Count>0)
        {
            mustQuery.Add(q=>q.Term(i=>i.Field("LogEvents.chainId").Value(input.ChainId)));
            mustQuery.Add(q => q.Range(i => i.Field("LogEvents.blockHeight").GreaterThanOrEquals(input.StartBlockHeight)));
            mustQuery.Add(q => q.Range(i => i.Field("LogEvents.blockHeight").LessThanOrEquals(input.EndBlockHeight)));

            if (input.IsOnlyConfirmed)
            {
                mustQuery.Add(q=>q.Term(i=>i.Field("LogEvents.isConfirmed").Value(input.IsOnlyConfirmed)));
            }
            
            var shouldQuery = new List<Func<QueryContainerDescriptor<TransactionIndex>, QueryContainer>>();
            foreach (var eventInput in input.Events)
            {
                var shouldMustQuery = new List<Func<QueryContainerDescriptor<TransactionIndex>, QueryContainer>>();
                if (!string.IsNullOrEmpty(eventInput.ContractAddress))
                {
                    shouldMustQuery.Add(s =>
                        s.Match(i =>
                            i.Field("LogEvents.contractAddress").Query(eventInput.ContractAddress)));
                }

                var shouldMushShouldQuery = new List<Func<QueryContainerDescriptor<TransactionIndex>, QueryContainer>>();
                if (eventInput.EventNames != null)
                {
                    foreach (var eventName in eventInput.EventNames)
                    {
                        if (!string.IsNullOrEmpty(eventName))
                        {
                            shouldMushShouldQuery.Add(s =>
                                s.Match(i => i.Field("LogEvents.eventName").Query(eventName)));
                        }
                    }
                }

                if (shouldMushShouldQuery.Count > 0)
                {
                    shouldMustQuery.Add(q => q.Bool(b => b.Should(shouldMushShouldQuery)));
                }

                shouldQuery.Add(q => q.Bool(b => b.Must(shouldMustQuery)));
            }

            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
            
            QueryContainer Filter(QueryContainerDescriptor<TransactionIndex> f) => f.Nested(q => q.Path("LogEvents")
                .Query(qq => qq.Bool(b => b.Must(mustQuery))));

            Func<SortDescriptor<TransactionIndex>, IPromise<IList<ISort>>> sort = s =>
                s.Ascending(a => a.BlockHeight).Ascending(d => d.Index);
            
            var list = await _transactionIndexRepository.GetSortListAsync(Filter, sortFunc:sort,limit:int.MaxValue);
            resultList = ObjectMapper.Map<List<TransactionIndex>, List<TransactionDto>>(list.Item2);
        }
        else
        {
            mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId).Value(input.ChainId)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).GreaterThanOrEquals(input.StartBlockHeight)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(input.EndBlockHeight)));
            
            if (input.IsOnlyConfirmed)
            {
                mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.IsConfirmed).Value(input.IsOnlyConfirmed)));
            }
            
            QueryContainer Filter(QueryContainerDescriptor<TransactionIndex> f) => f.Bool(b => b.Must(mustQuery));
            
            Func<SortDescriptor<TransactionIndex>, IPromise<IList<ISort>>> sort = s =>
                s.Ascending(a => a.BlockHeight).Ascending(d => d.Index);
            
            var list = await _transactionIndexRepository.GetSortListAsync(Filter,sortFunc:sort,limit:int.MaxValue);

            resultList = ObjectMapper.Map<List<TransactionIndex>, List<TransactionDto>>(list.Item2);
        }
        

        return resultList;
    }
    
    public async Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        if (input.EndBlockHeight - input.StartBlockHeight > _apiOptions.LogEventQueryHeightInterval)
        {
            input.EndBlockHeight = input.StartBlockHeight + _apiOptions.LogEventQueryHeightInterval;
        }
        
        var sortFuncs = new List<Func<SortDescriptor<TransactionIndex>, IPromise<IList<ISort>>>>();
        sortFuncs.Add(srt => srt.Field(sf => sf.Field(p => p.BlockHeight).Order(SortOrder.Ascending)));
        sortFuncs.Add(srt => srt.Field(sf => sf.Field(p => p.Index).Order(SortOrder.Ascending)));

        var mustQuery = new List<Func<QueryContainerDescriptor<LogEventIndex>, QueryContainer>>();
        mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).GreaterThanOrEquals(input.StartBlockHeight)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(input.EndBlockHeight)));

        if (input.IsOnlyConfirmed)
        {
            mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.IsConfirmed).Value(input.IsOnlyConfirmed)));
        }
        
        if (input.Events != null && input.Events.Count>0)
        {
            var shouldQuery = new List<Func<QueryContainerDescriptor<LogEventIndex>, QueryContainer>>();
            foreach (var eventInput in input.Events)
            {
                var shouldMustQuery = new List<Func<QueryContainerDescriptor<LogEventIndex>, QueryContainer>>();
                if (!string.IsNullOrEmpty(eventInput.ContractAddress))
                {
                    shouldMustQuery.Add(s =>
                        s.Match(i =>
                            i.Field(f=>f.ContractAddress).Query(eventInput.ContractAddress)));
                }

                var shouldMushShouldQuery = new List<Func<QueryContainerDescriptor<LogEventIndex>, QueryContainer>>();
                if (eventInput.EventNames != null)
                {
                    foreach (var eventName in eventInput.EventNames)
                    {
                        if (!string.IsNullOrEmpty(eventName))
                        {
                            shouldMushShouldQuery.Add(s =>
                                s.Match(i => i.Field(f=>f.EventName).Query(eventName)));
                        }
                    }
                }

                if (shouldMushShouldQuery.Count > 0)
                {
                    shouldMustQuery.Add(q => q.Bool(b => b.Should(shouldMushShouldQuery)));
                }

                shouldQuery.Add(q => q.Bool(b => b.Must(shouldMustQuery)));
            }

            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }


        QueryContainer Filter(QueryContainerDescriptor<LogEventIndex> f) => f.Bool(b => b.Must(mustQuery));
        List<LogEventDto> resultList = new List<LogEventDto>();

        Func<SortDescriptor<LogEventIndex>, IPromise<IList<ISort>>> sort = s =>
            s.Ascending(a => a.BlockHeight).Ascending(d => d.Index);

        var list = await _logEventIndexRepository.GetSortListAsync(Filter, sortFunc: sort,limit:int.MaxValue);

        resultList = ObjectMapper.Map<List<LogEventIndex>, List<LogEventDto>>(list.Item2);
        
        
        return resultList;
    }
    

}