using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElfScan.AElf;

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

    [Authorize]
    public async Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        if ((input.EndBlockNumber - input.StartBlockNumber) > _apiOptions.BlockQueryAmountInterval)
        {
            input.EndBlockNumber = input.StartBlockNumber + _apiOptions.BlockQueryAmountInterval;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();

        List<BlockDto> items = new List<BlockDto>();
        if (input.Events != null && input.Events.Count>0)
        {
            mustQuery.Add(q=>q.Term(i=>i.Field("Transactions.chainId").Value(input.ChainId)));
            // mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId.Suffix("keyword")).Value(input.ChainId)));
            mustQuery.Add(q => q.Range(i => i.Field("Transactions.blockNumber").GreaterThanOrEquals(input.StartBlockNumber)));
            mustQuery.Add(q => q.Range(i => i.Field("Transactions.blockNumber").LessThanOrEquals(input.EndBlockNumber)));

            if (input.IsOnlyConfirmed)
            {
                mustQuery.Add(q=>q.Term(i=>i.Field("Transactions.isConfirmed").Value(input.IsOnlyConfirmed)));
            }
            
            var shouldQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();
            foreach (var eventInput in input.Events)
            {
                var shouldMustQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();
                if (!string.IsNullOrEmpty(eventInput.ContractAddress))
                {
                    shouldMustQuery.Add(s =>
                        s.Match(i =>
                            i.Field("Transactions.LogEvents.contractAddress").Query(eventInput.ContractAddress)));
                }

                var shouldMushShouldQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();
                if (eventInput.EventNames != null)
                {
                    foreach (var eventName in eventInput.EventNames)
                    {
                        if (!string.IsNullOrEmpty(eventName))
                        {
                            shouldMushShouldQuery.Add(s =>
                                s.Match(i => i.Field("Transactions.LogEvents.eventName").Query(eventName)));
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
            
            QueryContainer Filter(QueryContainerDescriptor<BlockIndex> f) => f.Nested(q => q.Path("Transactions")
                        .Query(qq => qq.Bool(b => b.Must(mustQuery))));
            
            var list = await _blockIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockNumber,
                sortType:SortOrder.Ascending);
            items = ObjectMapper.Map<List<BlockIndex>, List<BlockDto>>(list.Item2);
        }
        else
        {
            mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId).Value(input.ChainId)));
            // mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId.Suffix("keyword")).Value(input.ChainId)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).GreaterThanOrEquals(input.StartBlockNumber)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).LessThanOrEquals(input.EndBlockNumber)));

            if (input.IsOnlyConfirmed)
            {
                mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.IsConfirmed).Value(input.IsOnlyConfirmed)));
            }
            
            QueryContainer Filter(QueryContainerDescriptor<BlockIndex> f) => f.Bool(b => b.Must(mustQuery));
            
            var list = await _blockIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockNumber,
                sortType:SortOrder.Ascending);
            items = ObjectMapper.Map<List<BlockIndex>, List<BlockDto>>(list.Item2);
        }

        List<BlockDto> resultList = new List<BlockDto>();
        if (!input.HasTransaction)
        {
            foreach (var blockItem in items)
            {
                blockItem.Transactions = null;
                resultList.Add(blockItem);
            }
        }
        else
        {
            resultList.AddRange(items);
        }
        
        return resultList;
    }

    [Authorize]
    public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        if (input.EndBlockNumber - input.StartBlockNumber > _apiOptions.BlockQueryAmountInterval)
        {
            input.EndBlockNumber = input.StartBlockNumber + _apiOptions.BlockQueryAmountInterval;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<TransactionIndex>, QueryContainer>>();
        
        List<TransactionDto> resultList = new List<TransactionDto>();
        if (input.Events != null && input.Events.Count>0)
        {
            mustQuery.Add(q=>q.Term(i=>i.Field("LogEvents.chainId").Value(input.ChainId)));
            mustQuery.Add(q => q.Range(i => i.Field("LogEvents.blockNumber").GreaterThanOrEquals(input.StartBlockNumber)));
            mustQuery.Add(q => q.Range(i => i.Field("LogEvents.blockNumber").LessThanOrEquals(input.EndBlockNumber)));

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
                s.Ascending(a => a.BlockNumber).Ascending(d => d.Index);
            
            var list = await _transactionIndexRepository.GetSortListAsync(Filter, sortFunc:sort);
            resultList = ObjectMapper.Map<List<TransactionIndex>, List<TransactionDto>>(list.Item2);
        }
        else
        {
            mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId).Value(input.ChainId)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).GreaterThanOrEquals(input.StartBlockNumber)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).LessThanOrEquals(input.EndBlockNumber)));
            
            if (input.IsOnlyConfirmed)
            {
                mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.IsConfirmed).Value(input.IsOnlyConfirmed)));
            }
            
            QueryContainer Filter(QueryContainerDescriptor<TransactionIndex> f) => f.Bool(b => b.Must(mustQuery));
            
            Func<SortDescriptor<TransactionIndex>, IPromise<IList<ISort>>> sort = s =>
                s.Ascending(a => a.BlockNumber).Ascending(d => d.Index);
            
            var list = await _transactionIndexRepository.GetSortListAsync(Filter,sortFunc:sort);

            resultList = ObjectMapper.Map<List<TransactionIndex>, List<TransactionDto>>(list.Item2);
        }
        

        return resultList;
    }

    [Authorize]
    public async Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        if (input.EndBlockNumber - input.StartBlockNumber > _apiOptions.BlockQueryAmountInterval)
        {
            input.EndBlockNumber = input.StartBlockNumber + _apiOptions.BlockQueryAmountInterval;
        }
        
        var sortFuncs = new List<Func<SortDescriptor<TransactionIndex>, IPromise<IList<ISort>>>>();
        sortFuncs.Add(srt => srt.Field(sf => sf.Field(p => p.BlockNumber).Order(SortOrder.Ascending)));
        sortFuncs.Add(srt => srt.Field(sf => sf.Field(p => p.Index).Order(SortOrder.Ascending)));

        var mustQuery = new List<Func<QueryContainerDescriptor<LogEventIndex>, QueryContainer>>();
        mustQuery.Add(q=>q.Term(i=>i.Field(f=>f.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).GreaterThanOrEquals(input.StartBlockNumber)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).LessThanOrEquals(input.EndBlockNumber)));

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
            s.Ascending(a => a.BlockNumber).Ascending(d => d.Index);

        var list = await _logEventIndexRepository.GetSortListAsync(Filter, sortFunc: sort);

        resultList = ObjectMapper.Map<List<LogEventIndex>, List<LogEventDto>>(list.Item2);
        
        
        return resultList;
    }
    

}