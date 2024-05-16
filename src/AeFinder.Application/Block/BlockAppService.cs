using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;
using AeFinder.Entities.Es;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace AeFinder.Block;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BlockAppService:ApplicationService,IBlockAppService
{
    private readonly IEntityMappingRepository<BlockIndex, string> _blockIndexRepository;
    private readonly IEntityMappingRepository<TransactionIndex, string> _transactionIndexRepository;
    private readonly IEntityMappingRepository<LogEventIndex, string> _logEventIndexRepository;
    private readonly ApiOptions _apiOptions;
    private readonly IEntityMappingRepository<SummaryIndex, string> _summaryIndexRepository;
    
    public BlockAppService(IEntityMappingRepository<BlockIndex, string> blockIndexRepository,
        IEntityMappingRepository<TransactionIndex,string> transactionIndexRepository,
        IEntityMappingRepository<LogEventIndex,string> logEventIndexRepository,
        IOptionsSnapshot<ApiOptions> apiOptions,
        IEntityMappingRepository<SummaryIndex, string> summaryIndexRepository)
    {
        _blockIndexRepository = blockIndexRepository;
        _transactionIndexRepository = transactionIndexRepository;
        _logEventIndexRepository = logEventIndexRepository;
        _apiOptions = apiOptions.Value;
        _summaryIndexRepository = summaryIndexRepository;
    }

    public async Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        if ((input.EndBlockHeight - input.StartBlockHeight + 1) > _apiOptions.BlockQueryHeightInterval)
        {
            input.EndBlockHeight = input.StartBlockHeight + _apiOptions.BlockQueryHeightInterval - 1;
        }
        List<BlockDto> items = new List<BlockDto>();
        Expression<Func<BlockIndex, bool>> expression = p =>
            p.ChainId == input.ChainId && p.BlockHeight >= input.StartBlockHeight &&
            p.BlockHeight <= input.EndBlockHeight;
        if (!string.IsNullOrEmpty(input.BlockHash))
        {
            expression = p =>
                p.ChainId == input.ChainId && p.BlockHash == input.BlockHash;
        }
        if (input.IsOnlyConfirmed)
        {
            expression = expression.And(p => p.Confirmed == input.IsOnlyConfirmed);
        }
        var queryable = await _blockIndexRepository.GetQueryableAsync();
        var list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(10000).ToList();
        if (list.Count == 10000)
        {
            list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(20000).ToList();
            if (list.Count == 20000)
            {
                list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(int.MaxValue).ToList();
            }
        }
        items = ObjectMapper.Map<List<BlockIndex>, List<BlockDto>>(list);
        List<BlockDto> resultList = new List<BlockDto>();
        resultList.AddRange(items);
        return resultList;
    }
    
    public async Task<long> GetBlockCountAsync(GetBlocksInput input)
    {
        Expression<Func<BlockIndex, bool>> expression = p =>
            p.ChainId == input.ChainId && p.BlockHeight >= input.StartBlockHeight &&
            p.BlockHeight <= input.EndBlockHeight;
        if (!string.IsNullOrEmpty(input.BlockHash))
        {
            expression = p =>
                p.ChainId == input.ChainId && p.BlockHash == input.BlockHash;
        }
        if (input.IsOnlyConfirmed)
        {
            expression = expression.And(p => p.Confirmed == input.IsOnlyConfirmed);
        }
    
        var count = await _blockIndexRepository.GetCountAsync(expression);
        
        return count;
    }
    
   public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        if ((input.EndBlockHeight - input.StartBlockHeight + 1) > _apiOptions.TransactionQueryHeightInterval)
        {
            input.EndBlockHeight = input.StartBlockHeight + _apiOptions.TransactionQueryHeightInterval - 1;
        }
        var queryable = await _transactionIndexRepository.GetQueryableAsync();

        List<TransactionDto> resultList = new List<TransactionDto>();
        if (input.Events != null && input.Events.Count>0)
        {
            Expression<Func<TransactionIndex, bool>> mustQuery = p=>p.LogEvents.Any(i=>i.ChainId == input.ChainId && i.BlockHeight >= input.StartBlockHeight && i.BlockHeight <= input.EndBlockHeight);
            if(!string.IsNullOrEmpty(input.TransactionId))
            {
                mustQuery = p=>p.LogEvents.Any(i=>i.ChainId == input.ChainId && i.TransactionId == input.TransactionId);
            }
            if (input.IsOnlyConfirmed)
            {
                mustQuery = mustQuery.And(p=>p.LogEvents.Any(i=>i.Confirmed == input.IsOnlyConfirmed));
            }
            
            Expression<Func<TransactionIndex, bool>> shouldQuery = null;
            foreach (var eventInput in input.Events)
            {
                Expression<Func<TransactionIndex, bool>> shouldMustQuery = null;
                if (!string.IsNullOrEmpty(eventInput.ContractAddress))
                {
                    shouldMustQuery = p=>p.LogEvents.Any(i=>i.ContractAddress == eventInput.ContractAddress);
                }

                Expression<Func<TransactionIndex, bool>> ShouldMustShouldQuery = null;
                if (eventInput.EventNames != null)
                {
                    foreach (var eventName in eventInput.EventNames)
                    {
                        if (!string.IsNullOrEmpty(eventName))
                        {
                            ShouldMustShouldQuery = ShouldMustShouldQuery is null
                                ? p => p.LogEvents.Any(i => i.EventName == eventName)
                                : ShouldMustShouldQuery.Or(p => p.LogEvents.Any(i => i.EventName == eventName));
                        }
                    }
                }

                if (ShouldMustShouldQuery != null)
                {
                    shouldMustQuery = shouldMustQuery is null ? ShouldMustShouldQuery : shouldMustQuery.And(ShouldMustShouldQuery);
                }

                shouldQuery = shouldQuery is null ? shouldMustQuery : shouldQuery.Or(shouldMustQuery);
               
            }
            mustQuery = mustQuery.And(shouldQuery);
            var list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(10000).ToList();
            if (list.Count == 10000)
            {
                list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(20000).ToList();
                if (list.Count == 20000)
                {
                    list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(int.MaxValue).ToList();
                }
            }
            resultList = ObjectMapper.Map<List<TransactionIndex>, List<TransactionDto>>(list);
        }
        else
        {
            Expression<Func<TransactionIndex, bool>> mustQuery = p => p.ChainId == input.ChainId && p.BlockHeight >= input.StartBlockHeight && p.BlockHeight <= input.EndBlockHeight;
            if (!string.IsNullOrEmpty(input.TransactionId))
            {
                mustQuery = p => p.ChainId == input.ChainId && p.TransactionId == input.TransactionId;
            }
            if (input.IsOnlyConfirmed)
            {
                mustQuery = mustQuery.And(p => p.Confirmed == input.IsOnlyConfirmed);
            }
            
            var list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(10000).ToList();

            if (list.Count == 10000)
            {
                list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(20000).ToList();

                if (list.Count == 20000)
                {
                    list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(int.MaxValue).ToList();
                }
            }
            resultList = ObjectMapper.Map<List<TransactionIndex>, List<TransactionDto>>(list);
        }
        

        return resultList;
    }
    
     public async Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        if ((input.EndBlockHeight - input.StartBlockHeight + 1) > _apiOptions.LogEventQueryHeightInterval)
        {
            input.EndBlockHeight = input.StartBlockHeight + _apiOptions.LogEventQueryHeightInterval - 1;
        }
        
        Expression<Func<LogEventIndex, bool>> mustQuery = p =>
            p.ChainId == input.ChainId && p.BlockHeight >= input.StartBlockHeight &&
            p.BlockHeight <= input.EndBlockHeight;

        if (input.IsOnlyConfirmed)
        {
            mustQuery = mustQuery.And(p => p.Confirmed == input.IsOnlyConfirmed);
        }
        
        if (input.Events != null && input.Events.Count>0)
        {
            Expression<Func<LogEventIndex, bool>> shouldQuery = null;
            foreach (var eventInput in input.Events)
            {
                Expression<Func<LogEventIndex, bool>> shouldMustQuery = null;
                if (!string.IsNullOrEmpty(eventInput.ContractAddress))
                {
                    shouldMustQuery = p => p.ContractAddress == eventInput.ContractAddress;
                }

                Expression<Func<LogEventIndex, bool>> shouldMushShouldQuery = null;
                if (eventInput.EventNames != null)
                {
                    foreach (var eventName in eventInput.EventNames)
                    {
                        if (!string.IsNullOrEmpty(eventName))
                        {
                            shouldMushShouldQuery = shouldMushShouldQuery is null ? p => p.EventName == eventName : shouldMushShouldQuery.Or(p => p.EventName == eventName);
                        }
                    }
                }

                if (shouldMushShouldQuery != null)
                {
                    shouldMustQuery = shouldMustQuery is null ? shouldMushShouldQuery : shouldMustQuery.And(shouldMushShouldQuery);
                }

                shouldQuery = shouldQuery is null ? shouldMustQuery : shouldQuery.Or(shouldMustQuery);
            }

            mustQuery = mustQuery.And(shouldQuery);
        }
        
        List<LogEventDto> resultList = new List<LogEventDto>();
        var queryable = await _logEventIndexRepository.GetQueryableAsync();
        var list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(10000).ToList();
        if (list.Count == 10000)
        {
            list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(20000).ToList();
            if (list.Count == 20000)
            {
                list = queryable.Where(mustQuery).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(int.MaxValue).ToList();
            }
        }
        
        resultList = ObjectMapper.Map<List<LogEventIndex>, List<LogEventDto>>(list);
        
        return resultList;
    }

     public async Task<List<TransactionDto>> GetSubscriptionTransactionsAsync(GetSubscriptionTransactionsInput input)
     {
         if ((input.EndBlockHeight - input.StartBlockHeight + 1) > _apiOptions.TransactionQueryHeightInterval)
        {
            input.EndBlockHeight = input.StartBlockHeight + _apiOptions.TransactionQueryHeightInterval - 1;
        }
        var queryable = await _transactionIndexRepository.GetQueryableAsync();

        var resultList = new List<TransactionDto>();
        Expression<Func<TransactionIndex, bool>> query = q => q.ChainId == input.ChainId && q.BlockHeight >= input.StartBlockHeight && q.BlockHeight <= input.EndBlockHeight;
        
        if (input.IsOnlyConfirmed)
        {
            query = query.And(p => p.Confirmed == input.IsOnlyConfirmed);
        }
        
        Expression<Func<TransactionIndex, bool>> transactionQuery = null;
        if (input.TransactionFilters != null && input.TransactionFilters.Count > 0)
        {
            foreach (var transactionInput in input.TransactionFilters)
            {
                Expression<Func<TransactionIndex, bool>> shouldMustQuery = null;
                if (!string.IsNullOrEmpty(transactionInput.To))
                {
                    shouldMustQuery = q => q.To == transactionInput.To;
                    
                    Expression<Func<TransactionIndex, bool>> ShouldMustShouldQuery = null;
                    foreach (var methodName in transactionInput.MethodNames)
                    {
                        if (!string.IsNullOrEmpty(methodName))
                        {
                            ShouldMustShouldQuery = ShouldMustShouldQuery is null
                                ? q => q.MethodName == methodName
                                : ShouldMustShouldQuery.Or(p => p.MethodName == methodName);
                        }
                    }
                    
                    if (ShouldMustShouldQuery != null)
                    {
                        shouldMustQuery = shouldMustQuery.And(ShouldMustShouldQuery);
                    }

                    transactionQuery = transactionQuery is null ? shouldMustQuery : transactionQuery.Or(shouldMustQuery);
                }
            }
        }

        Expression<Func<TransactionIndex, bool>> logEventQuery = null;
        if (input.LogEventFilters != null && input.LogEventFilters.Count>0)
        {
            foreach (var eventInput in input.LogEventFilters)
            {
                Expression<Func<TransactionIndex, bool>> shouldMustQuery = null;
                if (!string.IsNullOrEmpty(eventInput.ContractAddress))
                {
                    shouldMustQuery = q => q.LogEvents.Any(i => i.ContractAddress == eventInput.ContractAddress);

                    Expression<Func<TransactionIndex, bool>> ShouldMustShouldQuery = null;
                    foreach (var eventName in eventInput.EventNames)
                    {
                        if (!string.IsNullOrEmpty(eventName))
                        {
                            ShouldMustShouldQuery = ShouldMustShouldQuery is null
                                ? q => q.LogEvents.Any(i => i.EventName == eventName)
                                : ShouldMustShouldQuery.Or(p => p.LogEvents.Any(i => i.EventName == eventName));
                        }
                    }

                    if (ShouldMustShouldQuery != null)
                    {
                        shouldMustQuery = shouldMustQuery.And(ShouldMustShouldQuery);
                    }

                    logEventQuery = logEventQuery is null ? shouldMustQuery : logEventQuery.Or(shouldMustQuery);
                }

            }
        }

        if (transactionQuery != null && logEventQuery != null)
        {
            query = query.And(transactionQuery.Or(logEventQuery));
        }
        else if (transactionQuery != null || logEventQuery != null)
        {
            query = query.And(transactionQuery ?? logEventQuery);
        }

        var list = queryable.Where(query).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(10000).ToList();
        if (list.Count == 10000)
        {
            list = queryable.Where(query).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(20000).ToList();
            if (list.Count == 20000)
            {
                list = queryable.Where(query).OrderBy(p => p.BlockHeight).OrderBy(p => p.Index).Skip(0).Take(int.MaxValue).ToList();
            }
        }
        resultList = ObjectMapper.Map<List<TransactionIndex>, List<TransactionDto>>(list);
        

        return resultList;
     }

     public async Task<List<SummaryDto>> GetSummariesAsync(GetSummariesInput input)
     {
         var resultList = new List<SummaryDto>();
         Expression<Func<SummaryIndex, bool>> expression = p =>
             p.ChainId == input.ChainId;
         var queryable = await _summaryIndexRepository.GetQueryableAsync();
         var list = queryable.Where(expression).Skip(0).Take(100).ToList();
         resultList = ObjectMapper.Map<List<SummaryIndex>, List<SummaryDto>>(list);
         return resultList;
     }

     // public Task<List<BlockDto>> GetBlocksTestAsync(GetBlocksTestInput input)
     // {
     //     if ((input.EndBlockHeight - input.StartBlockHeight + 1) > _apiOptions.BlockQueryHeightInterval)
     //     {
     //         input.EndBlockHeight = input.StartBlockHeight + _apiOptions.BlockQueryHeightInterval - 1;
     //     }
     //     List<BlockDto> items = new List<BlockDto>();
     //     Expression<Func<BlockIndex, bool>> expression = p =>
     //         p.ChainId == input.ChainId && p.BlockHeight >= input.StartBlockHeight &&
     //         p.BlockHeight <= input.EndBlockHeight;
     //     if (!string.IsNullOrEmpty(input.BlockHash))
     //     {
     //         expression = p =>
     //             p.ChainId == input.ChainId && p.BlockHash == input.BlockHash;
     //     }
     //     if (input.IsOnlyConfirmed)
     //     {
     //         expression = expression.And(p => p.Confirmed == input.IsOnlyConfirmed);
     //     }
     //     var queryable = await _blockIndexRepository.GetQueryableAsync();
     //     var list = queryable.Where(expression).OrderBy(p => p.BlockHeight).After(new object[]{input.ResultCount,input.StartValue}).ToList();
     //
     //     // var list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(10000).ToList();
     //     if (list.Count == 10000)
     //     {
     //         list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(20000).ToList();
     //         if (list.Count == 20000)
     //         {
     //             list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(int.MaxValue).ToList();
     //         }
     //     }
     //     items = ObjectMapper.Map<List<BlockIndex>, List<BlockDto>>(list);
     //     List<BlockDto> resultList = new List<BlockDto>();
     //     resultList.AddRange(items);
     //     return resultList;
     // }

     public async Task<List<BlockDto>> GetBlocksTestAsync(GetBlocksTestInput input)
     {
         if ((input.EndBlockHeight - input.StartBlockHeight + 1) > _apiOptions.BlockQueryHeightInterval)
         {
             input.EndBlockHeight = input.StartBlockHeight + _apiOptions.BlockQueryHeightInterval - 1;
         }
         List<BlockDto> items = new List<BlockDto>();
         Expression<Func<BlockIndex, bool>> expression = p =>
             p.ChainId == input.ChainId && p.BlockHeight >= input.StartBlockHeight &&
             p.BlockHeight <= input.EndBlockHeight;
         if (!string.IsNullOrEmpty(input.BlockHash))
         {
             expression = p =>
                 p.ChainId == input.ChainId && p.BlockHash == input.BlockHash;
         }
         if (input.IsOnlyConfirmed)
         {
             expression = expression.And(p => p.Confirmed == input.IsOnlyConfirmed);
         }

         if (!string.IsNullOrEmpty(input.EndWithStr))
         {
             expression.And(p => p.ChainId.EndsWith(input.EndWithStr));
         }

         if (!string.IsNullOrEmpty(input.StartWithStr))
         {
             expression.And(p => p.ChainId.StartsWith(input.StartWithStr));
         }

         if (!string.IsNullOrEmpty(input.ContainsStr))
         {
             expression.And(p => p.Miner.Contains(input.ContainsStr));
         }

         var queryable = await _blockIndexRepository.GetQueryableAsync();
         var list = queryable.Where(expression).OrderByDescending(p=>p.ChainId).OrderBy(p => p.BlockHeight).After(new object[]{input.SearAfterCHainId, input.SearAfterBlockHeight}).ToList();

         // var list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(10000).ToList();
         // if (list.Count == 10000)
         // {
         //     list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(20000).ToList();
         //     if (list.Count == 20000)
         //     {
         //         list = queryable.Where(expression).OrderBy(p => p.BlockHeight).Skip(0).Take(int.MaxValue).ToList();
         //     }
         // }
         items = ObjectMapper.Map<List<BlockIndex>, List<BlockDto>>(list);
         List<BlockDto> resultList = new List<BlockDto>();
         resultList.AddRange(items);
         return resultList;
     }
}